// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
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
            Directory.CreateDirectory("moniker-definition-test");
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

        [Theory]
        [InlineData(@"{
    ""monikers"": [
        {
            ""name"": """",
            ""product"": ""product-test""
        }
    ]
}", "Invalid moniker definition file: Moniker name cannot be null or empty")]
        [InlineData(@"{
    ""monikers"": [
        {
            ""name"": ""netcore-1.0"",
            ""product"": """"
        }
    ]
}", "Invalid moniker definition file: Product name cannot be null or empty")]
        public void TestInvalidMonikerDefinitionShouldFail(string content, string errorMessage)
        {
            var path = $"moniker-definition-test/{Guid.NewGuid()}";

            File.WriteAllText(path, content);

            var exception = Assert.Throws<DocfxException>(() => MonikerRangeParser.Create(path));
            Assert.Equal("invalid-moniker-definition", exception.Error.Code);
            Assert.Equal(errorMessage, exception.Error.Message);
        }

        [Fact]
        public void TestNullDefinitionShouldFail()
        {
            var (_, monikerRangeParser) = MonikerRangeParser.Create(null);
            var exception = Assert.Throws<DocfxException>(() => monikerRangeParser.Parse("netcore-1.0"));
            Assert.Equal("invalid-moniker-range", exception.Error.Code);
            Assert.Equal("MonikerRange `netcore-1.0` is invalid: Moniker `netcore-1.0` is not defined", exception.Error.Message);
        }
    }
}
