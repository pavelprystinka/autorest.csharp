// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AutoRest.CSharp.Output.Models.Types;

namespace AutoRest.CSharp.Output.Models.Serialization.Xml
{
    internal class XmlObjectContentSerialization
    {
        public XmlObjectContentSerialization(ObjectTypeProperty property, XmlValueSerialization valueSerialization)
        {
            Property = property;
            ValueSerialization = valueSerialization;
        }

        public ObjectTypeProperty Property { get; }
        public XmlValueSerialization ValueSerialization { get; }
    }
}