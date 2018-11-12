// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Xunit;

namespace Microsoft.Docs.Build
{
    public class MonikerRangeParserTest
    {
        private readonly MonikerDefinitionModel _monikerDefinition = new MonikerDefinitionModel
        {
            Monikers =
            {
                new Moniker
                {
                    Name = "netcore-1.0",
                    Product = ".NET Core",
                },
                new Moniker
                {
                    Name = "netcore-2.0",
                    Product = ".NET Core",
                },
                new Moniker
                {
                    Name = "netcore-3.0",
                    Product = ".NET Core",
                },
                new Moniker
                {
                    Name = "dotnet-1.0",
                    Product = ".NET Framework",
                },
                new Moniker
                {
                    Name = "dotnet-2.0",
                    Product = ".NET Framework",
                },
                new Moniker
                {
                    Name = "dotnet-3.0",
                    Product = ".NET Framework",
                },
            }
        };

        private readonly MonikerRangeParser _monikerRangeParser;

        public MonikerRangeParserTest()
        {
            _monikerRangeParser = new MonikerRangeParser(_monikerDefinition);
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
             "dotnet-3.0 netcore-1.0")]
        [InlineData(
            "dotnet-3.0 || netcore-1.0",
             "dotnet-3.0 netcore-1.0")]
        [InlineData(
            ">netcore-1.0<netcore-3.0",
            "netcore-2.0")]
        [InlineData(
            ">NETCORE-1.0 <NETcore-3.0",
            "netcore-2.0")]
        [InlineData(
            "netcore-1.0<netcore-3.0",
            "netcore-1.0")]
        [InlineData(
            ">= netcore-1.0 < netcore-2.0 || dotnet-3.0",
            "dotnet-3.0 netcore-1.0")]
        [InlineData(
            ">= netcore-2.0 || > dotnet-2.0",
            "dotnet-3.0 netcore-2.0 netcore-3.0")]
        public void TestEvaluateMonikerRange(string rangeString, string expectedMonikers)
        {
            var result = _monikerRangeParser.Parse(rangeString);
            Assert.True(Enumerable.SequenceEqual(expectedMonikers.Split(' ', StringSplitOptions.RemoveEmptyEntries), result));
        }

        [Theory]
        [InlineData("netcore-xp", "Moniker `netcore-xp` is not defined")]
        [InlineData("netcore-1.0 < || netcore-2.0", "Expect a moniker string, but got ` || netcore-2.0`")]
        [InlineData(">netcore&-1.0", "Parse ends before reaching end of string, unrecognized string: `&-1.0`")]
        [InlineData(">=>netcore&-1.0", "Expect a moniker string, but got `>netcore&-1.0`")]
        [InlineData(">netcore<-1.0", "Moniker `netcore` is not defined")]
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
            var monikerDefinition = new MonikerDefinitionModel
            {
                Monikers =
                {
                    new Moniker
                    {
                        Name = "netcore-1.0",
                        Product = ".NET Core",
                    },
                    new Moniker
                    {
                        Name = "netcore-1.0",
                        Product = ".NET Core",
                    },
                   new Moniker
                    {
                        Name = "netcore-2.0",
                        Product = ".NET Core",
                    },
                }
            };

            var exception = Assert.Throws<DocfxException>(() => new MonikerRangeParser(monikerDefinition));
            Assert.Equal("moniker-name-conflict", exception.Error.Code);
            Assert.Equal("Two or more moniker definitions have the same monikerName `netcore-1.0`", exception.Error.Message);
        }

        [Fact]
        public void TestNullDefinitionShouldFail()
        {
            var monikerRangeParser = new MonikerRangeParser(new MonikerDefinitionModel());
            var exception = Assert.Throws<DocfxException>(() => monikerRangeParser.Parse("netcore-1.0"));
            Assert.Equal("invalid-moniker-range", exception.Error.Code);
            Assert.Equal("MonikerRange `netcore-1.0` is invalid: Moniker `netcore-1.0` is not defined", exception.Error.Message);
        }
    }
}
