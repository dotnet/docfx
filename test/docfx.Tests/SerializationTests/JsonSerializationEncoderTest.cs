// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using FluentAssertions;

namespace docfx.Tests;

[TestClass]
public partial class JsonSerializationEncoderTest
{
    [TestMethod]
    [DataRow("abcdefghighlmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    [DataRow("0123456789")]
    [DataRow("\0\a\b\t\n\v\f\r\e")]
    [DataRow("!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~")]
    [DataRow("①②③")] // NonAscii chars (Enclosed Alphanumerics)
    [DataRow("１２３")] // NonAscii chars (Full-width digits)
    [DataRow("äöü")]   // Umlaut
    [DataRow("漢字")]   // Kanji
    public void JsonEncoderTest(string data)
    {
        // Arrange
        var model = data;

        //Act
        var systemTextJsonResult = SystemTextJsonUtility.Serialize(model);
        var newtonsoftJsonResult = NewtonsoftJsonUtility.Serialize(model);

        // Assert
        // Compare serialized result text with `StringComparer.OrdinalIgnoreCase`
        //  - SystemTextJson escape chars with capital case(`\u001B`)
        //  - NewtonsoftJson escape chars with lower case  (`\u001b`)
        ((object)systemTextJsonResult).Should().Be(newtonsoftJsonResult, StringComparer.OrdinalIgnoreCase); // Currently StringAssertions don't expose overload that accepts StringComparer. (See: https://github.com/fluentassertions/fluentassertions/issues/2720)
    }

    [TestMethod]
    [DataRow("　", @"\u3000")]                                 // Full-Width space (Excaped by global block list (https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/character-encoding#global-block-list))
    [DataRow("𠮟", @"\uD842\uDF9F")]                           // Kanji (that use Surrogate Pair)
    [DataRow("📄", @"\uD83D\uDCC4")]                          // Emoji
    [DataRow("👁‍🗨", @"\uD83D\uDC41‍\uD83D\uDDE8")] // Emoji (with ZWJ (ZERO WIDTH JOINER))
    public void JsonEncoderTest_NoCompatibility(string data, string expected)
    {
        // Arrange
        var model = data;

        //Act
        var systemTextJsonResult = SystemTextJsonUtility.Serialize(model);
        var newtonsoftJsonResult = NewtonsoftJsonUtility.Serialize(model);

        // Assert
        systemTextJsonResult.Should().NotBe(newtonsoftJsonResult);

        systemTextJsonResult.Should().Contain(expected);
        newtonsoftJsonResult.Should().Contain(data);
    }
}
