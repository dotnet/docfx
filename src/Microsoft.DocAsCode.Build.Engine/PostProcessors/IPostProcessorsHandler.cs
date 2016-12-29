// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Plugins;

    internal interface IPostProcessorsHandler
    {
        void Handle(List<PostProcessor> postProcessors, Manifest manifest, string outputFolder);
    }
}
