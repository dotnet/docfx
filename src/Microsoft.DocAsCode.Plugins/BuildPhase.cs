// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;

    public enum BuildPhase
    {
        Compile,
        Link,
        [Obsolete]
        PreBuildBuild = Compile,
        [Obsolete]
        PostBuild = Link,
    }
}
