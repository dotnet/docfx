// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.TypeInspectors
{
    using System;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public interface IExtensibleTypeInspector : ITypeInspector
    {
        IPropertyDescriptor GetProperty(Type type, object container, string name);
    }
}
