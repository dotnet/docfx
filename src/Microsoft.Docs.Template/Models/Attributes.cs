// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Exports a type to be processed by build pipeline into a JSON model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DataSchemaAttribute : Attribute { }

    /// <summary>
    /// Exports a type to be processed by build pipeline into an HTML page.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PageSchemaAttribute : DataSchemaAttribute
    {
        public bool Contributors { get; }

        public bool GitUrl { get; }

        public bool DocumentId { get; }

        public bool Toc { get; }

        public PageSchemaAttribute(
            bool contributors = true,
            bool gitUrl = true,
            bool documentId = true,
            bool toc = true)
        {
            Contributors = contributors;
            GitUrl = gitUrl;
            DocumentId = documentId;
            Toc = toc;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public abstract class SchemaFeatureAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class DataTypeAttribute : Attribute
    {
        public virtual Type TargetType => typeof(string);
    }

    public class HrefAttribute : DataTypeAttribute { }

    public class MarkdownAttribute : DataTypeAttribute { }

    public class InlineMarkdownAttribute : DataTypeAttribute { }
}
