namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class ResolveLink : IResolverPipeline
    {
        public ParseResult Run(MetadataModel yaml, ResolverContext context)
        {
            var index = yaml.Indexer;

            TreeIterator.Preorder(yaml.TocYamlViewModel, null,
                s => s.IsInvalid ? null : s.Items,
                (member, parent) =>
                {
                    // get all the possible places where link is possible
                    member.Remarks = ResolveText(index, member.Remarks, member);
                    member.Summary = ResolveText(index, member.Summary, member);
                    if (member.Syntax != null && member.Syntax.Parameters != null)
                        member.Syntax.Parameters.ForEach(s =>
                        {
                            s.Description = ResolveText(index, s.Description, member);
                        });
                    if (member.Syntax != null && member.Syntax.Return != null)
                        member.Syntax.Return.Description = ResolveText(index, member.Syntax.Return.Description, member);

                    // resolve parameter's Type
                    return true;
                });

            return new ParseResult(ResultLevel.Success);
        }

        private static string ResolveText(ApiReferenceModel dict, string input, MetadataItem currentMember)
        {
            if (string.IsNullOrEmpty(input) || dict == null) return input;
            return LinkParser.ResolveToMarkdownLink((s) =>
            {
                ApiIndexItemModel item;
                // Step1. Search for item that with the fullname exists in the dictionary
                if (dict.TryGetValue(s, out item)) return item;

                // Step2. Search for item that in the same namespace with current api, and exists in the dictionary
                var fullName = currentMember.NamespaceName + "." + s;
                if (dict.TryGetValue(fullName, out item)) return item;
                return null;
            }, input, (s) =>
            {
                return MetadataModelUtility.ResolveApiHrefRelativeToCurrentApi(dict, s.Name, currentMember.Name);
            });
        }
    }
}
