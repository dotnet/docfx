namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.DocAsCode.Utility;

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
                                        SetHref(item, yaml.Indexer, current.Name);
                                    }
                                }
                            }
                        }
                    }
                    return true;
                });

            return new ParseResult(ResultLevel.Success);
        }

        private static void SetHref(LinkItem s, ApiReferenceModel index, string currentName)
        {
            if (s == null) return;
            if (!s.IsExternalPath)
            {
                s.Href = ResolveInternalLink(index, s.Name, currentName);
            }
            else
            {
                // Set ExternalPath to null;
                s.Href = null;
            }
        }

        private static string ResolveInternalLink(ApiReferenceModel index, string name, string currentName)
        {
            return MetadataModelUtility.ResolveApiHrefRelativeToCurrentApi(index, name, currentName);
        }
    }
}
