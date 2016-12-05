// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Linq;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public class SetDerivedClass: IResolverPipeline
    {
        private readonly Dictionary<string, List<string>> _derivedClassMapping = new Dictionary<string, List<string>>();

        public void Run(MetadataModel yaml, ResolverContext context)
        {
            InitDerivedClassMapping(yaml.Members);
            AppendDerivedClass(yaml.Members);
        }

        private void InitDerivedClassMapping(List<MetadataItem> items)
        {
            foreach (var item in items ?? Enumerable.Empty<MetadataItem>())
            {
                var inheritance = item.Inheritance;
                if (inheritance?.Any() == true)
                {
                    List<string> derivedClass;
                    if (_derivedClassMapping.TryGetValue(inheritance.Last(), out derivedClass))
                    {
                        derivedClass.Add(item.Name);
                    }
                    else
                    {
                        _derivedClassMapping.Add(inheritance.Last(), new List<string> { item.Name });
                    }
                }
            }
        }

        private void AppendDerivedClass(List<MetadataItem> items)
        {
            foreach (var item in items ?? Enumerable.Empty<MetadataItem>())
            {
                if (item.Type == MemberType.Class)
                {
                    List<string> derivedClass;
                    if (_derivedClassMapping.TryGetValue(item.Name, out derivedClass))
                    {
                        item.DerivedClass = derivedClass;
                    }
                }
            }
        }
    }
}