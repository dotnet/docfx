// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.DataContracts.ManagedReference;

namespace Docfx.Dotnet;

internal class NormalizeSyntax : IResolverPipeline
{
    public void Run(MetadataModel yaml, ResolverContext context)
    {
        TreeIterator.Preorder(
            yaml.TocYamlViewModel,
            null,
            s => s.IsInvalid ? null : s.Items,
            (member, parent) =>
            {
                // get all the possible places where link is possible
                if (member.Syntax is { Content: not null })
                {
                    SyntaxLanguage[] keys = new SyntaxLanguage[member.Syntax.Content.Count];
                    member.Syntax.Content.Keys.CopyTo(keys, 0);
                    foreach (var key in keys)
                    {
                        member.Syntax.Content[key] = NormalizeLines(member.Syntax.Content[key]);
                    }
                }

                return true;
            });
    }

    private static string NormalizeLines(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }
        return content.Replace("\r\n", "\n");
    }
}
