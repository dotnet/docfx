// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System;

    [Serializable]
    public enum MemberType
    {
        Default,
        Toc,
        Package,
        Namespace,
        Class,
        Interface,
        Enum,
        Field,
        Property,
        Event,
        Constructor,
        Method,
    }
}
