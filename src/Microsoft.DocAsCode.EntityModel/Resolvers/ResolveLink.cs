// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class ResolveLink : IResolverPipeline
    {
        public ParseResult Run(MetadataModel yaml, ResolverContext context)
        {
            if (context.PreserveRawInlineComments) return new ParseResult(ResultLevel.Success);

            TreeIterator.Preorder(yaml.TocYamlViewModel, null,
                s => s.IsInvalid ? null : s.Items,
                (member, parent) =>
                {
                    // get all the possible places where link is possible
                    member.Remarks = ResolveText(member.Remarks, member);
                    member.Summary = ResolveText(member.Summary, member);
                    member.Example = ResolveText(member.Example, member);
                    if (member.Syntax != null && member.Syntax.Parameters != null)
                        member.Syntax.Parameters.ForEach(s =>
                        {
                            s.Description = ResolveText(s.Description, member);
                        });

                    if (member.Exceptions != null)
                    {
                        member.Exceptions.ForEach(s => {
                            s.Description = ResolveText(s.Description, member);
                        });
                    }

                    if (member.Sees != null)
                    {
                        member.Sees.ForEach(s => {
                            s.Description = ResolveText(s.Description, member);
                        });
                    }

                    if (member.SeeAlsos != null)
                    {
                        member.SeeAlsos.ForEach(s => {
                            s.Description = ResolveText(s.Description, member);
                        });
                    }

                    if (member.Syntax != null && member.Syntax.Return != null)
                        member.Syntax.Return.Description = ResolveText(member.Syntax.Return.Description, member);

                    // resolve parameter's Type
                    return true;
                });

            return new ParseResult(ResultLevel.Success);
        }
        
        private static string ResolveText(string input, MetadataItem currentMember)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return LinkParser.ResolveToXref(input);
        }
    }
}
