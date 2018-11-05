// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Xunit;

namespace Microsoft.Docs.Build
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
            _monikerRangeParser = new MonikerRangeParser(_monikers);
        }

        [Theory]
        [InlineData(
            "netcore-1.0 netcore-3.0",
            "")]
        [InlineData(
            " netcore-1.0 ",
            "netcore-1.0")]
        [InlineData(
            "netcore-1.0 || dotnet-3.0",
             "netcore-1.0 dotnet-3.0")]
        [InlineData(
            ">netcore-1.0<netcore-3.0",
            "netcore-2.0")]
        [InlineData(
            "netcore-1.0<netcore-3.0",
            "netcore-1.0")]
        [InlineData(
            ">= netcore-1.0 < netcore-2.0 || dotnet-3.0",
            "netcore-1.0 dotnet-3.0")]
        [InlineData(
            ">= netcore-2.0 || > dotnet-2.0",
            "netcore-3.0 netcore-2.0 dotnet-3.0")]
        public void TestEvaluateMonikerRange(string rangeString, string expectedMonikers)
        {
            var result = _monikerRangeParser.Parse(rangeString);
            Enumerable.SequenceEqual(expectedMonikers.Split(' ', StringSplitOptions.RemoveEmptyEntries), result);
        }

        [Theory]
        [InlineData("netcore-xp", "Moniker `netcore-xp` is not found in available monikers list")]
        [InlineData("netcore-1.0 < || netcore-2.0", "Expect a moniker string, but got ` || netcore-2.0`")]
        [InlineData(">netcore&-1.0", "Parse ends before reaching end of string, unrecognized string: `&-1.0`")]
        [InlineData(">=>netcore&-1.0", "Expect a moniker string, but got `>netcore&-1.0`")]
        [InlineData(">netcore<-1.0", "Moniker `netcore` is not found in available monikers list")]
        [InlineData(">netcore<-1.0 ||| >netcore-2.0", "Expect a comparator set, but got `| >netcore-2.0`")]
        [InlineData(">netcore<-1.0 || ||", "Expect a comparator set, but got ` ||`")]
        [InlineData(">netcore<-1.0 || <", "Expect a moniker string, but got ``")]
        public void InvalidMonikerRange(string rangeString, string errorMessage)
        {
            var exception = Assert.Throws<DocfxException>(() => _monikerRangeParser.Parse(rangeString));
            Assert.Equal("invalid-moniker-range", exception.Error.Code);
            Assert.Equal(errorMessage, exception.Error.Message.Substring($"MonikerRange `{rangeString}` is invalid: ".Length));
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
            var exception = Assert.Throws<DocfxException>(() => new MonikerRangeParser(monikers));
            Assert.Equal("moniker-name-conflict", exception.Error.Code);
            Assert.Equal("Two or more moniker definitions have the same monikerName `netcore-1.0`", exception.Error.Message);
        }
    }
}
