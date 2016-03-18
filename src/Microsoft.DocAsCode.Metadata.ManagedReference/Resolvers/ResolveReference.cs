// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Utility;

    public class ResolveReference : IResolverPipeline
    {
        public void Run(MetadataModel yaml, ResolverContext context)
        {
            TreeIterator.Preorder(yaml.TocYamlViewModel, null,
                s => s.IsInvalid ? null : s.Items,
                (current, parent) =>
                {
                    MetadataItem page;
                    var addingReferences = new List<ReferenceItem>();
                    var documentReferences = current.References;
                    if (current.Type.IsPageLevel())
                    {
                        page = current;
                        current.References = new Dictionary<string, ReferenceItem>();
                    }
                    else
                    {
                        page = parent;
                        current.References = null;
                    }
                    if (documentReferences != null && documentReferences.Count > 0)
                    {
                        foreach (var key in documentReferences.Keys)
                        {
                            TryAddReference(context, page, addingReferences, key);
                        }
                    }
                    foreach (var key in GetReferenceKeys(current))
                    {
                        TryAddReference(context, page, addingReferences, key);
                    }
                    if (current.Type == MemberType.Namespace)
                    {
                        foreach (var item in current.Items)
                        {
                            TryAddReference(context, page, addingReferences, item.Name);
                        }
                    }
                    AddIndirectReference(context, page, addingReferences);
                    return true;
                });
        }

        private static void TryAddReference(ResolverContext context, MetadataItem page, List<ReferenceItem> addingReferences, string key)
        {
            if (!page.References.ContainsKey(key))
            {
                ReferenceItem item;
                if (context.References.TryGetValue(key, out item))
                {
                    var reference = context.References[key].Clone();
                    page.References.Add(key, reference);
                    addingReferences.Add(reference);
                }
                else
                {
                    Debug.Fail(string.Format("Reference not found: {0}", key));
                }
            }
        }

        private void AddIndirectReference(ResolverContext context, MetadataItem page, List<ReferenceItem> addedReferences)
        {
            while (addedReferences.Count > 0)
            {
                var addingReferences = new List<ReferenceItem>();
                foreach (var r in addedReferences)
                {
                    foreach (var key in GetReferenceKeys(r))
                    {
                        TryAddReference(context, page, addingReferences, key);
                    }
                }
                addedReferences = addingReferences;
            }
        }

        private IEnumerable<string> GetReferenceKeys(MetadataItem current)
        {
            if (current.NamespaceName != null)
            {
                yield return current.NamespaceName;
            }

            if (current.Overridden != null)
            {
                yield return current.Overridden;
            }

            if (current.Inheritance?.Count > 0)
            {
                foreach (var item in current.Inheritance)
                {
                    yield return item;
                }
            }

            if (current.Implements?.Count > 0)
            {
                foreach (var item in current.Implements)
                {
                    yield return item;
                }
            }

            if (current.InheritedMembers?.Count > 0)
            {
                foreach (var item in current.InheritedMembers)
                {
                    yield return item;
                }
            }

            if (current.Exceptions?.Count > 0)
            {
                foreach (var item in current.Exceptions)
                {
                    yield return item.Type;
                }
            }

            if (current.Sees?.Count > 0)
            {
                foreach (var item in current.Sees)
                {
                    yield return item.Type;
                }
            }

            if (current.SeeAlsos?.Count > 0)
            {
                foreach (var item in current.SeeAlsos)
                {
                    yield return item.Type;
                }
            }

            if (current.Syntax != null)
            {
                if (current.Syntax.Parameters?.Count > 0)
                {
                    foreach (var item in current.Syntax.Parameters)
                    {
                        yield return item.Type;
                    }
                }

                if (current.Syntax.Return != null)
                {
                    yield return current.Syntax.Return.Type;
                }
            }
        }

        private IEnumerable<string> GetReferenceKeys(ReferenceItem reference)
        {
            if (reference.Definition != null)
            {
                yield return reference.Definition;
            }

            if (reference.Parent != null)
            {
                yield return reference.Parent;
            }
        }
    }
}
