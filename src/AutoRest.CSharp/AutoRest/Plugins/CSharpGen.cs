﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoRest.CSharp.AutoRest.Communication;
using AutoRest.CSharp.Generation.Types;
using AutoRest.CSharp.Generation.Writers;
using AutoRest.CSharp.Input;
using AutoRest.CSharp.Input.Source;
using AutoRest.CSharp.Output.Builders;
using AutoRest.CSharp.Output.Models;
using AutoRest.CSharp.Output.Models.Responses;
using AutoRest.CSharp.Output.Models.Types;
using AutoRest.CSharp.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Diagnostic = Microsoft.CodeAnalysis.Diagnostic;

namespace AutoRest.CSharp.AutoRest.Plugins
{
    [PluginName("csharpgen")]
    internal class CSharpGen : IPlugin
    {
        public async Task<GeneratedCodeWorkspace> ExecuteAsync(Task<CodeModel> codeModelTask, Configuration configuration)
        {
            Directory.CreateDirectory(configuration.OutputFolder);
            var projectDirectory = Path.Combine(configuration.OutputFolder, Configuration.ProjectRelativeDirectory);
            var project = await GeneratedCodeWorkspace.Create(projectDirectory, configuration.OutputFolder, configuration.SharedSourceFolders);
            var sourceInputModel = new SourceInputModel(await project.GetCompilationAsync());

            var context = new BuildContext(await codeModelTask, configuration, sourceInputModel);

            var modelWriter = new ModelWriter();
            var clientWriter = new ClientWriter();
            var restClientWriter = new RestClientWriter();
            var serializeWriter = new SerializationWriter();
            var headerModelModelWriter = new ResponseHeaderGroupWriter();

            foreach (var model in context.Library.Models)
            {
                var codeWriter = new CodeWriter();
                modelWriter.WriteModel(codeWriter, model);

                var serializerCodeWriter = new CodeWriter();
                serializeWriter.WriteSerialization(serializerCodeWriter, model);

                var name = model.Type.Name;
                project.AddGeneratedFile($"Models/{name}.cs", codeWriter.ToString());
                project.AddGeneratedFile($"Models/{name}.Serialization.cs", serializerCodeWriter.ToString());
            }

            foreach (var client in context.Library.RestClients)
            {
                var restCodeWriter = new CodeWriter();
                restClientWriter.WriteClient(restCodeWriter, client);

                project.AddGeneratedFile($"{client.Type.Name}.cs", restCodeWriter.ToString());
            }

            foreach (ResponseHeaderGroupType responseHeaderModel in context.Library.HeaderModels)
            {
                var headerModelCodeWriter = new CodeWriter();
                headerModelModelWriter.WriteHeaderModel(headerModelCodeWriter, responseHeaderModel);

                project.AddGeneratedFile($"{responseHeaderModel.Type.Name}.cs", headerModelCodeWriter.ToString());
            }

            if (configuration.PublicClients && !configuration.AzureArm && context.Library.Clients.Count() > 0)
            {
                var codeWriter = new CodeWriter();
                ClientOptionsWriter.WriteClientOptions(codeWriter, context);

                var clientOptionsName = ClientBase.GetClientPrefix(context.DefaultLibraryName, context);
                project.AddGeneratedFile($"{clientOptionsName}ClientOptions.cs", codeWriter.ToString());
            }

            foreach (var client in context.Library.Clients)
            {
                var codeWriter = new CodeWriter();
                clientWriter.WriteClient(codeWriter, client, context);

                project.AddGeneratedFile($"{client.Type.Name}.cs", codeWriter.ToString());
            }

            foreach (var operation in context.Library.LongRunningOperations)
            {
                var codeWriter = new CodeWriter();
                LongRunningOperationWriter.Write(codeWriter, operation);

                project.AddGeneratedFile($"{operation.Type.Name}.cs", codeWriter.ToString());
            }

            if (context.Configuration.AzureArm)
            {
                var codeWriter = new CodeWriter();
                ManagementClientWriter.WriteClientOptions(codeWriter, context);
                var libraryName = ManagementClientWriter.GetManagementClientPrefix(context.DefaultLibraryName);
                project.AddGeneratedFile($"{libraryName}ManagementClientOptions.cs", codeWriter.ToString());

                var clientCodeWriter = new CodeWriter();
                ManagementClientWriter.WriteAggregateClient(clientCodeWriter, context);
                project.AddGeneratedFile($"{libraryName}ManagementClient.cs", clientCodeWriter.ToString());

            }

            return project;
        }

        public async Task<bool> Execute(IPluginCommunication autoRest)
        {
            string codeModelFileName = (await autoRest.ListInputs()).FirstOrDefault();
            if (string.IsNullOrEmpty(codeModelFileName))
                throw new Exception("Generator did not receive the code model file.");

            var configuration = Configuration.GetConfiguration(autoRest);

            string codeModelYaml = string.Empty;

            Task<CodeModel> codeModelTask = Task.Run(async () =>
            {
                codeModelYaml = await autoRest.ReadFile(codeModelFileName);
                return CodeModelSerialization.DeserializeCodeModel(codeModelYaml);
            });

            if (configuration.CredentialTypes.Contains("TokenCredential", StringComparer.OrdinalIgnoreCase) &&
                configuration.CredentialScopes.Length < 1)
            {
                await autoRest.Fatal("You are using TokenCredential wihtout passing in any credential-scopes.");
                return false;
            }

            if (!Path.IsPathRooted(configuration.OutputFolder))
            {
                await autoRest.Warning("output-folder path should be an absolute path");
            }
            if (configuration.SaveInputs)
            {
                await codeModelTask;
                await autoRest.WriteFile("Configuration.json", StandaloneGeneratorRunner.SaveConfiguration(configuration), "source-file-csharp");
                await autoRest.WriteFile("CodeModel.yaml", codeModelYaml, "source-file-csharp");
            }

            var project = await ExecuteAsync(codeModelTask, configuration);
            await foreach (var file in project.GetGeneratedFilesAsync())
            {
                await autoRest.WriteFile(file.Name, file.Text, "source-file-csharp");
            }

            return true;
        }
    }
}
