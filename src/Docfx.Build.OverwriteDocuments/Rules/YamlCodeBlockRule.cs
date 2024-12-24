// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Markdig.Syntax;

namespace Docfx.Build.OverwriteDocuments;

public sealed class YamlCodeBlockRule : IOverwriteBlockRule
{
    public string TokenName => "YamlCodeBlock";

    private static readonly List<string> _allowedLanguages = ["yaml", "yml"];

    public bool Parse(Block block, out string value)
    {
        ArgumentNullException.ThrowIfNull(block);

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
