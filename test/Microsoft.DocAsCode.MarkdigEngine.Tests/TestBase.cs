// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class TestBase : IDisposable
    {
        public TestBase()
        {
            EnvironmentContext.FileAbstractLayerImpl = FileAbstractLayerBuilder.Default.ReadFromRealFileSystem(".").WriteToRealFileSystem(".").Create();
        }

        public void Dispose()
        {
            EnvironmentContext.Clean();
        }
    }
}
