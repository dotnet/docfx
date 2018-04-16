// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class BuildTest
    {
        public static readonly TheoryData<string> Specs = new TheoryData<string>();

        static BuildTest()
        {
            foreach (var spec in Directory.EnumerateFiles("specs", "*.yml", SearchOption.AllDirectories))
            {
                Specs.Add(spec);
            }
        }

        [Theory]
        [MemberData(nameof(Specs))]
        public static async Task BuildDocset(string specPath)
        {
            var spec = new BuildTestSpec();

            await Build.Run(
                ".",
                new CommandLineOptions(),
                new Context(new TestFileSystem(spec), new TestLog()));
        }
    }
}
