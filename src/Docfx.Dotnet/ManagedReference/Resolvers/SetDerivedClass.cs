// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Docfx.DataContracts.ManagedReference;

namespace Docfx.Dotnet;

internal class SetDerivedClass : IResolverPipeline
{
    private readonly Dictionary<string, List<string>> _derivedClassMapping = [];

    public void Run(MetadataModel yaml, ResolverContext context)
    {
        if (yaml.Members is { Count: > 0 })
        {
            UpdateDerivedClassMapping(yaml.Members, context.References);
            AppendDerivedClass(yaml.Members);
        }
    }

    private void UpdateDerivedClassMapping(List<MetadataItem> items, Dictionary<string, ReferenceItem> reference)
    {
        foreach (var item in items ?? Enumerable.Empty<MetadataItem>())
        {
            // Handle class inheritance
            var inheritance = item.Inheritance;
            if (inheritance is { Count: > 0 })
            {
                var superClass = inheritance[inheritance.Count - 1];

                if (reference.TryGetValue(superClass, out ReferenceItem referenceItem))
                {
                    superClass = referenceItem.Definition ?? superClass;
                }

                // ignore System.Object's derived class
                if (superClass != "System.Object")
                {
                    ref var derivedClasses = ref CollectionsMarshal.GetValueRefOrAddDefault(_derivedClassMapping, superClass, out var exists);
                    if (exists)
                        derivedClasses.Add(item.Name);
                    else
                        derivedClasses = [item.Name];
                }
            }

            // Handle interface implementations
            var implements = item.Implements;
            if (implements is { Count: > 0 })
            {
                var superClass = implements[implements.Count - 1];

                if (reference.TryGetValue(superClass, out var referenceItem))
                {
                    superClass = referenceItem.Definition ?? superClass;
                }

                ref var derivedClasses = ref CollectionsMarshal.GetValueRefOrAddDefault(_derivedClassMapping, superClass, out var exists);
                if (exists)
                    derivedClasses.Add(item.Name);
                else
                    derivedClasses = [item.Name];
            }
        }
    }

    private void AppendDerivedClass(List<MetadataItem> items)
    {
        foreach (var item in items ?? Enumerable.Empty<MetadataItem>())
        {
            switch (item.Type)
            {
                case MemberType.Class:
                case MemberType.Interface:
                    if (_derivedClassMapping.TryGetValue(item.Name, out List<string> derivedClasses))
                    {
                        derivedClasses.Sort();
                        item.DerivedClasses = derivedClasses;
                    }
                    continue;
            }
        }
    }
}
