﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;

    public class ModelManifest
    {
        public Dictionary<string, string> Models { get; } = new Dictionary<string, string>();
    }
}
