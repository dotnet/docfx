// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Test
{
    internal class TestFileSystem : IFileSystem
    {
        private readonly BuildSpec _spec;

        public TestFileSystem(BuildSpec spec) => _spec = spec;
    }
}
