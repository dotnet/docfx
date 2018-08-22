// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Exports a type to be used for schema document processing
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DataSchemaAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class DataTypeAttribute : Attribute
    {
        public Type RequiredType { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class HrefAttribute : DataTypeAttribute
    {
        public HrefAttribute() => RequiredType = typeof(string);
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MarkdownAttribute : DataTypeAttribute
    {
        public MarkdownAttribute(Type type) => RequiredType = typeof(string);
    }
}
