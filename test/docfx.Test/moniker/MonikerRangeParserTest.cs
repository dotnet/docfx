// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

using Xunit;

namespace Microsoft.Docs.Build.moniker
{
    public class MonikerRangeParserTest
    {
        private readonly Moniker[] _monikers =
        {
            new Moniker
            {
                MonikerName = "netcore-1.0",
                Order = 1,
                ProductName = ".NET Core",
            },
            new Moniker
            {
                MonikerName = "netcore-2.0",
                Order = 2,
                ProductName = ".NET Core",
            },
            new Moniker
            {
                MonikerName = "netcore-3.0",
                Order = 3,
                ProductName = ".NET Core",
            },
            new Moniker
            {
                MonikerName = "dotnet-1.0",
                Order = 1,
                ProductName = ".NET Framework",
            },
            new Moniker
            {
                MonikerName = "dotnet-2.0",
                Order = 2,
                ProductName = ".NET Framework",
            },
            new Moniker
            {
                MonikerName = "dotnet-3.0",
                Order = 3,
                ProductName = ".NET Framework",
            },
        };

        private readonly MonikerRangeParser _monikerRangeParser;

        public MonikerRangeParserTest()
        {
            (_, _monikerRangeParser) = MonikerRangeParser.Create(_monikers);
        }

        [Theory]
        [InlineData(
            "netcore-1.0 netcore-3.0",
            new string[0])]
        [InlineData(
            " netcore-1.0 ",
            new[] { "netcore-1.0" })]
        [InlineData(
            "netcore-1.0 || dotnet-3.0",
            new[] { "netcore-1.0", "dotnet-3.0" })]
        [InlineData(
            ">netcore-1.0<netcore-3.0",
            new[] { "netcore-2.0" })]
        [InlineData(
            ">= netcore-1.0 < netcore-2.0 || dotnet-3.0",
            new[] { "netcore-1.0", "dotnet-3.0" })]
        [InlineData(
            ">= netcore-2.0 || > dotnet-2.0",
            new[] { "netcore-3.0", "netcore-2.0", "dotnet-3.0" })]
        public void TestEvaluateMonikerRange(string rangeString, string[] expectedMonikers)
        {
            var (errors, result) = _monikerRangeParser.Parse(rangeString);
            Assert.Empty(errors);
            Assert.Equal(expectedMonikers.Length, result.ToArray().Length);
            foreach (var moniker in result)
            {
                Assert.Contains(moniker, expectedMonikers);
            }
        }

        [Fact]
        public void TestMonikerNotInMonikerListShouldFail()
        {
            var (errors, result) = _monikerRangeParser.Parse("netcore-xp");
            Assert.Null(result);
            Assert.Single(errors);
            Assert.Equal("MonikerRange `netcore-xp` is invalid: Moniker `netcore-xp` is not found in available monikers list", errors[0].Message);
        }

        [Fact]
        public void TestDuplicateMonikerNameShouldFail()
        {
            Moniker[] monikers =
            {
                new Moniker
                {
                    MonikerName = "netcore-1.0",
                    Order = 1,
                    ProductName = ".NET Core",
                },
                new Moniker
                {
                    MonikerName = "netcore-1.0",
                    Order = 2,
                    ProductName = ".NET Core",
                },
               new Moniker
                {
                    MonikerName = "netcore-2.0",
                    Order = 2,
                    ProductName = ".NET Core",
                },
            };
            var (errors, monikerRangeParser) = MonikerRangeParser.Create(monikers);
            Assert.Single(errors);
            Assert.Equal("Two or more moniker definitions have the same monikerName `netcore-1.0`", errors[0].Message);
        }
    }
}
