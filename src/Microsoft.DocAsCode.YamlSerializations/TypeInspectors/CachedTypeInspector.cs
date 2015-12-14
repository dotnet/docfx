// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerializations.TypeInspectors
{
    using System;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.TypeInspectors;

    /// <summary>
    /// Wraps another <see cref="ITypeInspector"/> and applies caching.
    /// </summary>
    public sealed class CachedTypeInspector : TypeInspectorSkeleton
    {
        private readonly ITypeInspector _innerTypeDescriptor;
        private readonly Dictionary<Type, List<IPropertyDescriptor>> _cache =
            new Dictionary<Type, List<IPropertyDescriptor>>();

        public CachedTypeInspector(ITypeInspector innerTypeDescriptor)
        {
            if (innerTypeDescriptor == null)
            {
                throw new ArgumentNullException("innerTypeDescriptor");
            }
            _innerTypeDescriptor = innerTypeDescriptor;
        }

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        {
            List<IPropertyDescriptor> list;
            if (!_cache.TryGetValue(type, out list))
            {
                list = new List<IPropertyDescriptor>(_innerTypeDescriptor.GetProperties(type, container));
                _cache[type] = list;
            }
            return list;
        }
    }
}
