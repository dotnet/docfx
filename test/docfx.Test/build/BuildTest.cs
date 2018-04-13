// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Test
{
    public static class BuildTest
    {
        [Fact]
        public static async Task BuildDocset()
        {
            await Build.Run(".", new CommandLineOptions(), new Context(new TestFileSystem(), new TestLog()));
        }
    }
}
