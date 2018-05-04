// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class MarkdownContext
    {
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

        public bool EnableValidation { get; }

        public ImmutableHashSet<string> InclusionSet { get; }

        public HashSet<string> Dependencies { get; } = new HashSet<string>();

        public bool EnableSourceInfo { get; }

        public MarkdownValidatorBuilder Mvb { get; }

        public IReadOnlyDictionary<string, string> Tokens { get; }

        public MarkdownContext(
            string basePath,
            string filePath,
            bool isInline,
            ImmutableHashSet<string> inclusionSet,
            HashSet<string> dependencies,
            bool enableSourceInfo,
            IReadOnlyDictionary<string, string> tokens,
            MarkdownValidatorBuilder mvb,
            bool enableValidation = false)
        {
            BasePath = basePath;
            FilePath = filePath;
            IsInline = isInline;
            EnableValidation = enableValidation;
            InclusionSet = inclusionSet ?? ImmutableHashSet<string>.Empty;
            Dependencies = dependencies ?? new HashSet<string>();

            Tokens = tokens;
            Mvb = mvb;
            EnableSourceInfo = enableSourceInfo;
        }
    }
}
