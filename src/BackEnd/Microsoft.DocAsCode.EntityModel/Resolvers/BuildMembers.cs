namespace Microsoft.DocAsCode.EntityModel
{
    using System.Threading.Tasks;

    public class BuildMembers : IResolverPipeline
    {
        public ParseResult Run(MetadataModel yaml, ResolverContext context)
        {
            TreeIterator.Preorder(yaml.TocYamlViewModel, null,
                s =>
                {
                    if (s.IsInvalid || (s.Type != MemberType.Namespace && s.Type != MemberType.Toc)) return null;
                    else return s.Items;
                },
                (member, parent) =>
                {
                    if (member.Type != MemberType.Toc)
                    {
                        yaml.Members.Add(member);
                    }

                    return true;
                });

            return new ParseResult(ResultLevel.Success);
        }
    }
}
