// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System;

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
}
