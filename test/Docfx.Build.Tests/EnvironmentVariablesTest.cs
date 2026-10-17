// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using EnvironmentVariables = Docfx.DataContracts.Common.Constants.EnvironmentVariables;

namespace Docfx.Build.Engine.Tests;

[Collection("docfx STA")]
public class EnvironmentVariablesTest
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData(" true ", true)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    [InlineData("invalid", false)]
    public void NoCheckCertificateRevocationListParsesBooleanValues(string value, bool expected)
    {
        var name = EnvironmentVariables.DOCFX_NO_CHECK_CERTIFICATE_REVOCATION_LIST;
        var original = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            Assert.Equal(expected, EnvironmentVariables.NoCheckCertificateRevocationList);
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, original);
        }
    }
}
