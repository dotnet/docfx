namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.EntityModel.ViewModels;

    public class ResolvePath : IResolverPipeline
    {
        public ParseResult Run(MetadataModel yaml, ResolverContext context)
        {
            TreeIterator.Preorder(yaml.TocYamlViewModel, null,
                s => s.IsInvalid ? null : s.Items,
                (current, parent) =>
                {
                    if (current.References != null)
                    {
                        foreach (var referenceItem in current.References.Values)
                        {
                            if (referenceItem.Parts != null)
                            {
                                foreach (var links in referenceItem.Parts.Values)
                                {
                                    foreach (var item in links)
                                    {
                                        SetHref(item, yaml.Indexer, current.Name, context);
                                    }
                                }
                            }
                        }
                    }
                    return true;
                });

            return new ParseResult(ResultLevel.Success);
        }

        private static void SetHref(LinkItem s, ApiReferenceModel index, string currentName, ResolverContext context)
        {
            if (s == null) return;
            if (!s.IsExternalPath)
            {
                s.Href = ResolveInternalLink(index, s.Name, currentName);
            }
            else
            {
                s.Href = ResolveExternalLink(s.Name, context);
            }
        }

        private static string ResolveInternalLink(ApiReferenceModel index, string name, string currentName)
        {
            return MetadataModelUtility.ResolveApiHrefRelativeToCurrentApi(index, name, currentName);
        }

        private static string ResolveExternalLink(string name, ResolverContext context)
        {
            if (context.ExternalReferences != null)
            {
                foreach (var reader in context.ExternalReferences)
                {
                    ReferenceViewModel vm;
                    if (reader.TryGetReference(name, out vm))
                    {
                        return vm.Href;
                    }
                }
            }
            return null;
        }
    }
}
