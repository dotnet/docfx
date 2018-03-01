// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    using Markdig.Syntax;

    public sealed class YamlCodeBlockRule : IOverwriteBlockRule
    {
        public string TokenName => "YamlCodeBlock";

        private static readonly List<string> _allowedLanguages = new List<string> { "yaml", "yml" };

        public bool Parse(Block block, out string value)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            var fenced = block as FencedCodeBlock;
            if (!string.IsNullOrEmpty(fenced?.Info) && !_allowedLanguages.Contains(fenced.Info.ToLower()))
            {
                Logger.LogWarning(
                    $"Unexpected language of fenced code block for YamlCodeBlock: {fenced.Info}.",
                    line: fenced.Lines.ToString(),
                    code: WarningCodes.Overwrite.InvalidYamlCodeBlockLanguage);
            }
            value = fenced?.Lines.ToString();
            return fenced != null;
        }
    }
}
