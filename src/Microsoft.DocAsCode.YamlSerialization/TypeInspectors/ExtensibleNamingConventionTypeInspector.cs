// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.TypeInspectors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using YamlDotNet.Serialization;

    public sealed class ExtensibleNamingConventionTypeInspector : ExtensibleTypeInspectorSkeleton
    {
        private readonly IExtensibleTypeInspector innerTypeDescriptor;
        private readonly INamingConvention namingConvention;

        public ExtensibleNamingConventionTypeInspector(IExtensibleTypeInspector innerTypeDescriptor, INamingConvention namingConvention)
        {
            if (innerTypeDescriptor == null)
            {
                throw new ArgumentNullException(nameof(innerTypeDescriptor));
            }

            this.innerTypeDescriptor = innerTypeDescriptor;

            if (namingConvention == null)
            {
                throw new ArgumentNullException(nameof(namingConvention));
            }

            this.namingConvention = namingConvention;
        }

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container) =>
            from p in innerTypeDescriptor.GetProperties(type, container)
            select (IPropertyDescriptor)new PropertyDescriptor(p) { Name = namingConvention.Apply(p.Name) };

        public override IPropertyDescriptor GetProperty(Type type, object container, string name) =>
            innerTypeDescriptor.GetProperty(type, container, name);
    }
}
