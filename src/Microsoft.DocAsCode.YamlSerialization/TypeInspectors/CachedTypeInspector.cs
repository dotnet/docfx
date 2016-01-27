// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.TypeInspectors
{
    using System;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    /// <summary>
    /// Wraps another <see cref="ITypeInspector"/> and applies caching.
    /// </summary>
    public sealed class CachedTypeInspector : ExtensibleTypeInspectorSkeleton
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

        protected override IEnumerable<IPropertyDescriptor> GetPropertiesCore(Type type, object container)
        {
            List<IPropertyDescriptor> list;
            if (!_cache.TryGetValue(type, out list))
            {
                list = new List<IPropertyDescriptor>(_innerTypeDescriptor.GetProperties(type, container));
                _cache[type] = list;
            }
            return list;
        }

        public override IPropertyDescriptor GetProperty(Type type, object container, string name)
        {
            return (_innerTypeDescriptor as IExtensibleTypeInspector)?.GetProperty(type, container, name);
        }

        public override IEnumerable<string> GetKeys(Type type, object container)
        {
            return (_innerTypeDescriptor as IExtensibleTypeInspector)?.GetKeys(type, container);
        }
    }
}
