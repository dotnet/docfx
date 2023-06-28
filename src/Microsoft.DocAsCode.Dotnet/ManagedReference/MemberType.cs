// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference;

[Serializable]
public enum MemberType
{
    Default,
    Toc,
    Assembly,
    Namespace,
    Class,
    Interface,
    Struct,
    Delegate,
    Enum,
    Field,
    Property,
    Event,
    Constructor,
    Method,
    Operator,
    Container,
    AttachedEvent,
    AttachedProperty
}
