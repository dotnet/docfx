// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.ObjectGraphVisitors
{
    using System;
    using System.ComponentModel;
    using YamlDotNet.Core;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.ObjectGraphVisitors;

    /// <summary>
    /// YamlDotNet behavior has changed since 6.x so a custom version which doesn't check on EnterMapping(IObjectDescriptor).
    /// </summary>
    internal sealed class ExclusiveObjectGraphVisitor : ChainedObjectGraphVisitor
    {
        public ExclusiveObjectGraphVisitor(IObjectGraphVisitor<IEmitter> nextVisitor)
            : base(nextVisitor)
        {
        }

        private static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context)
        {
            var defaultValueAttribute = key.GetCustomAttribute<DefaultValueAttribute>();
            object defaultValue = defaultValueAttribute != null
                ? defaultValueAttribute.Value
                : GetDefault(key.Type);

            return !Equals(value.Value, defaultValue) && base.EnterMapping(key, value, context);
        }
    }
}
