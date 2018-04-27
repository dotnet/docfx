// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Collections.Generic;

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

        public HashSet<string> Dependencies { get; } = new HashSet<string>();

        public bool EnableSourceInfo { get; }

        public MarkdownValidatorBuilder Mvb { get; }

        public IReadOnlyDictionary<string, string> Tokens { get; }

        public MarkdownContext(
            string content,
            string basePath,
            string filePath,
            bool isInline,
            IEnumerable<string> dependencies,
            bool enableSourceInfo,
            IReadOnlyDictionary<string, string> tokens,
            MarkdownValidatorBuilder mvb)
        {
            Content = content;
            BasePath = basePath;
            FilePath = filePath;
            IsInline = isInline;

            if (dependencies != null)
            {
                foreach (var dep in dependencies)
                {
                    Dependencies.Add(dep);
                }
            }

            Tokens = tokens;
            Mvb = mvb;
            EnableSourceInfo = enableSourceInfo;
        }
    }
}
