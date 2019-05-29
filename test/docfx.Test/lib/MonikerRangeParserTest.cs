// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                    MonikerName = "dotnet-3.0",
                    ProductName = ".NET Framework",
                    Order = 1,
                },
                new Moniker
                {
                    MonikerName = "dotnet-1.0",
                    ProductName = ".NET Framework",
                },
                new Moniker
                {
                    MonikerName = "dotnet-2.0",
                    ProductName = ".NET Framework",
                },
                new Moniker
                {
                    MonikerName = "netcore-1.0",
                    ProductName = ".NET Core",
                    Order = 1,
                },
                new Moniker
                {
                    MonikerName = "netcore-3.0",
                    ProductName = ".NET Core",
                    Order = 3
                },
                new Moniker
                {
                    MonikerName = "netcore-2.0",
                    ProductName = ".NET Core",
                    Order = 2
                },
            }
        };

        private readonly MonikerRangeParser _monikerRangeParser;
        private readonly MonikerComparer _monikerComparer;

        public MonikerRangeParserTest()
        {
            var monikersEvaluator = new EvaluatorWithMonikersVisitor(_monikerDefinition);
            _monikerRangeParser = new MonikerRangeParser(monikersEvaluator);
            _monikerComparer = new MonikerComparer(monikersEvaluator.MonikerOrder);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("  ", "")]
        [InlineData("", "")]
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
            var result = _monikerRangeParser.Parse(new SourceInfo<string>(rangeString)).ToList();
            result.Sort(_monikerComparer);
            Assert.Equal(expectedMonikers, string.Join(' ', result));
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
            var exception = Assert.Throws<DocfxException>(() => _monikerRangeParser.Parse(new SourceInfo<string>(rangeString)));
            Assert.Equal("moniker-range-invalid", exception.Error.Code);
            Assert.Equal(errorMessage, exception.Error.Message.Substring($"Invalid moniker range: '{rangeString}': ".Length));
        }

        [Fact]
        public void TestNullDefinitionShouldFail()
        {
            var monikerRangeParser = new MonikerRangeParser(new EvaluatorWithMonikersVisitor(new MonikerDefinitionModel()));
            var exception = Assert.Throws<DocfxException>(() => monikerRangeParser.Parse(new SourceInfo<string>("netcore-1.0")));
            Assert.Equal("moniker-range-invalid", exception.Error.Code);
            Assert.Equal("Invalid moniker range: 'netcore-1.0': Moniker `netcore-1.0` is not defined", exception.Error.Message);
        }
    }
}
