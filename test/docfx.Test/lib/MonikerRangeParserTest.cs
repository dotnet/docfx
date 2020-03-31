// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
                    MonikerName = "DOTNET-1.0",
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
                new Moniker
                {
                    MonikerName = "azuresqldb-current",
                    ProductName = "sql",
                    Order = 2
                },
                new Moniker
                {
                    MonikerName = "azure-sqldw-latest",
                    ProductName = "sql"
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
             "netcore-1.0 dotnet-3.0")]
        [InlineData(
            "Netcore-1.0",
             "netcore-1.0")]
        [InlineData(
            "dotnet-3.0 || netcore-1.0",
             "netcore-1.0 dotnet-3.0")]
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
            "netcore-1.0 dotnet-3.0")]
        [InlineData(
            ">= netcore-2.0 || > dotnet-2.0",
            "netcore-2.0 netcore-3.0 dotnet-3.0")]
        [InlineData(
            ">= netcore-2.0 > dotnet-2.0",
            "")]
        [InlineData(
            "azuresqldb-current || azure-sqldw-latest",
            "azure-sqldw-latest azuresqldb-current")]
        public void TestEvaluateMonikerRange(string rangeString, string expectedMonikers)
        {
            var (_, monikers) = _monikerRangeParser.Parse(new SourceInfo<string>(rangeString));
            var result = monikers.ToList();
            result.Sort(_monikerComparer);
            Assert.Equal(expectedMonikers, string.Join(' ', result));
        }

        [Theory]
        [InlineData("netcore-xp", "Invalid moniker range 'netcore-xp': Moniker 'netcore-xp' is not defined")]
        [InlineData("netcore-1.0 < || netcore-2.0", "Expect a moniker string, but got ' || netcore-2.0'")]
        [InlineData(">netcore&-1.0", "Parse ends before reaching end of string, unrecognized string: '&-1.0'")]
        [InlineData(">=>netcore&-1.0", "Expect a moniker string, but got '>netcore&-1.0'")]
        [InlineData(">netcore<-1.0", "Invalid moniker range '>netcore<-1.0': Moniker 'netcore' is not defined")]
        [InlineData(">netcore<-1.0 ||| >netcore-2.0", "Expect a comparator set, but got '| >netcore-2.0'")]
        [InlineData(">netcore<-1.0 || ||", "Expect a comparator set, but got ' ||'")]
        [InlineData(">netcore<-1.0 || <", "Expect a moniker string, but got ''")]
        public void InvalidMonikerRange(string rangeString, string errorMessage)
        {
            var (errors,_) = _monikerRangeParser.Parse(new SourceInfo<string>(rangeString));
            Assert.Contains(Errors.Versioning.MonikerRangeInvalid(null, errorMessage), errors, new ErrorEqualityComparer());
            //Assert.Contains(errors, error =>
            //{
            //    Assert.Equal("moniker-range-invalid", error.Code);
            //    Assert.Equal(errorMessage, error.Message);
            //});
        }

        [Fact]
        public void TestNullDefinitionShouldFail()
        {
            var monikerRangeParser = new MonikerRangeParser(new EvaluatorWithMonikersVisitor(new MonikerDefinitionModel()));
            var (errors, _) = monikerRangeParser.Parse(new SourceInfo<string>("netcore-1.0"));
            Assert.Collection(errors, error =>
            {
                Assert.Equal("moniker-range-invalid", error.Code);
                Assert.Equal("Invalid moniker range 'netcore-1.0': Moniker 'netcore-1.0' is not defined", error.Message);
            });
        }

        private class ErrorEqualityComparer : IEqualityComparer<Error>
        {
            public bool Equals(Error x, Error y)
            {
                return x.Level == y.Level &&
                       x.Code == y.Code &&
                       x.Message == y.Message;
            }

            public int GetHashCode(Error obj)
            {
                return HashCode.Combine(
                    obj.Level,
                    obj.Code,
                    obj.Message);
            }
        }
    }
}
