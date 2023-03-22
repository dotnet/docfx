// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.YamlSerialization.TypeInspectors;

public interface IExtensibleTypeInspector : ITypeInspector
{
    IPropertyDescriptor GetProperty(Type type, object container, string name);
}
