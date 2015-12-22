// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.EntityModel.ViewModels;

    public class ResolvePath : IResolverPipeline
    {
        public void Run(MetadataModel yaml, ResolverContext context)
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
            if (context.ExternalReferences == null)
            {
                return null;
            }
            ReferenceViewModel vm;
            context.ExternalReferences.TryGetReference(name, out vm);
            return vm?.Href;
        }
    }
}
