// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Collections.Immutable;

    public class MarkdownContext
    {
        /// <summary>
        /// Content of current markdown file.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Absolute path of `~`, the directory contains docfx.json.
        /// </summary>
        public string BasePath { get; }

        /// <summary>
        /// Relative path of current markdown file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Indicate if this file is inline included.
        /// </summary>
        public bool IsInline { get; }

        public ImmutableHashSet<string> InclusionSet { get; }

        public MarkdownValidatorBuilder Mvb { get; }

        public MarkdownContext(string filePath,
            string basePath,
            MarkdownValidatorBuilder mvb,
            string content,
            bool isInline,
            ImmutableHashSet<string> inclusionSet)
        {
            Content = content;
            FilePath = filePath;
            BasePath = basePath;
            Mvb = mvb;
            IsInline = isInline;
            InclusionSet = inclusionSet;
        }
    }
}
