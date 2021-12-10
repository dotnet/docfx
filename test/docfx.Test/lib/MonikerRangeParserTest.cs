// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build;

public class MonikerRangeParserTest
{
    private readonly MonikerDefinitionModel _monikerDefinition = new()
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
                    Order = 3,
                },
                new Moniker
                {
                    MonikerName = "netcore-2.0",
                    ProductName = ".NET Core",
                    Order = 2,
                },
                new Moniker
                {
                    MonikerName = "azuresqldb-current",
                    ProductName = "sql",
                    Order = 2,
                },
                new Moniker
                {
                    MonikerName = "azure-sqldw-latest",
                    ProductName = "sql",
                },
            },
    };

    private readonly MonikerRangeParser _monikerRangeParser;

    public MonikerRangeParserTest()
    {
        _monikerRangeParser = new(_monikerDefinition);
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
        "Netcore-1.0",
        "netcore-1.0")]
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
    [InlineData(
        ">= netcore-2.0 > dotnet-2.0",
        "")]
    [InlineData(
        "azuresqldb-current || azure-sqldw-latest",
        "azure-sqldw-latest azuresqldb-current")]
    public void TestEvaluateMonikerRange(string rangeString, string expectedMonikers)
    {
        var monikers = _monikerRangeParser.Parse(new ErrorList(), new SourceInfo<string>(rangeString));
        var result = monikers.ToList();
        result.Sort(StringComparer.Ordinal);
        Assert.Equal(expectedMonikers, string.Join(' ', result));
    }

    [Theory]
    [InlineData("netcore-xp", "Invalid moniker range: 'netcore-xp'. Moniker 'netcore-xp' is not defined.")]
    [InlineData("netcore-1.0 < || netcore-2.0", "Invalid moniker range: Expect a moniker string, but got ' || netcore-2.0'.")]
    [InlineData(">netcore&-1.0", "Invalid moniker range: Parse ends before reaching end of string, unrecognized string: '&-1.0'.")]
    [InlineData(">=>netcore&-1.0", "Invalid moniker range: Expect a moniker string, but got '>netcore&-1.0'.")]
    [InlineData(">netcore<-1.0", "Invalid moniker range: '>netcore<-1.0'. Moniker 'netcore' is not defined.")]
    [InlineData(">netcore<-1.0 ||| >netcore-2.0", "Invalid moniker range: Expect a comparator set, but got '| >netcore-2.0'.")]
    [InlineData(">netcore<-1.0 || ||", "Invalid moniker range: Expect a comparator set, but got ' ||'.")]
    [InlineData(">netcore<-1.0 || <", "Invalid moniker range: Expect a moniker string, but got ''.")]
    public void InvalidMonikerRange(string rangeString, string errorMessage)
    {
        var errors = new ErrorList();
        _monikerRangeParser.Parse(errors, new SourceInfo<string>(rangeString));
        Assert.Contains(errorMessage, errors.ToArray().Select(x => x.Message));
    }

    [Fact]
    public void TestNullDefinitionShouldFail()
    {
        var errors = new ErrorList();
        var monikerRangeParser = new MonikerRangeParser(new MonikerDefinitionModel());
        monikerRangeParser.Parse(errors, new SourceInfo<string>("netcore-1.0"));
        Assert.Collection(errors.ToArray(), error =>
        {
            Assert.Equal("moniker-range-invalid", error.Code);
            Assert.Equal("Invalid moniker range: 'netcore-1.0'. Moniker 'netcore-1.0' is not defined.", error.Message);
        });
    }
}
