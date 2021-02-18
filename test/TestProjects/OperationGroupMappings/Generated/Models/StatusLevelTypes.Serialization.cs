// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

using System;

namespace OperationGroupMappings
{
    internal static partial class StatusLevelTypesExtensions
    {
        public static string ToSerialString(this StatusLevelTypes value) => value switch
        {
            StatusLevelTypes.Info => "Info",
            StatusLevelTypes.Warning => "Warning",
            StatusLevelTypes.Error => "Error",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown StatusLevelTypes value.")
        };

        public static StatusLevelTypes ToStatusLevelTypes(this string value)
        {
            if (string.Equals(value, "Info", StringComparison.InvariantCultureIgnoreCase)) return StatusLevelTypes.Info;
            if (string.Equals(value, "Warning", StringComparison.InvariantCultureIgnoreCase)) return StatusLevelTypes.Warning;
            if (string.Equals(value, "Error", StringComparison.InvariantCultureIgnoreCase)) return StatusLevelTypes.Error;
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown StatusLevelTypes value.");
        }
    }
}
