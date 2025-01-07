// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.DataContracts.ManagedReference;

namespace Docfx.Dotnet;

internal class ResolveReference : IResolverPipeline
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
                    current.References = [];
                }
                else
                {
                    page = parent;
                    current.References = null;
                }
                if (documentReferences is { Count: > 0 })
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
            if (context.References.TryGetValue(key, out ReferenceItem item))
            {
                var reference = item.Clone();
                page.References.Add(key, reference);
                addingReferences.Add(reference);
            }
        }
    }

    private static void AddIndirectReference(ResolverContext context, MetadataItem page, List<ReferenceItem> addedReferences)
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

    private static IEnumerable<string> GetReferenceKeys(MetadataItem current)
    {
        if (current.NamespaceName != null)
        {
            yield return current.NamespaceName;
        }

        if (current.Overridden != null)
        {
            yield return current.Overridden;
        }

        if (current.Overload != null)
        {
            yield return current.Overload;
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

        if (current.DerivedClasses?.Count > 0)
        {
            foreach (var item in current.DerivedClasses)
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

        if (current.ExtensionMethods?.Count > 0)
        {
            foreach (var item in current.ExtensionMethods)
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

        if (current.SeeAlsos?.Count > 0)
        {
            foreach (var item in current.SeeAlsos.Where(l => l.LinkType == LinkType.CRef))
            {
                yield return item.LinkId;
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

    private static IEnumerable<string> GetReferenceKeys(ReferenceItem reference)
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
