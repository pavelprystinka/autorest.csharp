// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.Json;

namespace body_complex.Models.V20160229
{
    public partial class Shark
    {
        internal void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            if (Age != null)
            {
                writer.WritePropertyName("age");
                writer.WriteNumberValue(Age.Value);
            }
            writer.WritePropertyName("birthday");
            Azure.Core.Utf8JsonWriterExtensions.WriteStringValue(writer, Birthday, "S");
            writer.WriteEndObject();
        }
        internal static Shark Deserialize(JsonElement element)
        {
            var result = new Shark();
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("age"))
                {
                    result.Age = property.Value.GetInt32();
                    continue;
                }
                if (property.NameEquals("birthday"))
                {
                    result.Birthday = Azure.Core.TypeFormatters.GetDateTimeOffset(property.Value, "S");
                    continue;
                }
            }
            return result;
        }
    }
}