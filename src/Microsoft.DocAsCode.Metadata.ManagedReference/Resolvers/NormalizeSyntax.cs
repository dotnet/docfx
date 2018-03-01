// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public class NormalizeSyntax : IResolverPipeline
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
                    if (member.Syntax != null && member.Syntax.Content != null)
                    {
                        SyntaxLanguage[] keys = new SyntaxLanguage[member.Syntax.Content.Count];
                        member.Syntax.Content.Keys.CopyTo(keys, 0);
                        foreach(var key in keys)
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
}
