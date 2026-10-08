// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Xunit;

#nullable enable

namespace Docfx.Dotnet.Tests;

public partial class SymbolStringComparerTest
{
    [Fact]
    public void Compare_SameReference()
    {
        string str = "test";
        var result = SymbolStringComparer.Instance.Compare(str, str);
        result.Should().Be(0);
    }

    [Theory]
    [InlineData("_", "_")]
    [InlineData("a", "a")]
    [InlineData("Z", "Z")]
    [InlineData("test", "test")]
    [InlineData("", "")]
    [InlineData("①", "①")]
    [InlineData(null, null)]
    public void CompareEquals(string? value1, string? value2)
    {
        // Act
        var result = SymbolStringComparer.Instance.Compare(value1, value2);

        // Assert
        result.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(TestData.StringPatterns), MemberType = typeof(TestData))]
    public void Compare_String_Order(string? value1, string? value2)
    {
        // Test forward order
        {
            // Act
            var result = SymbolStringComparer.Instance.Compare(value1, value2);

            // Assert
            result.Should().BeLessThan(0);
        }

        // Test reverse order
        {
            // Act
            var result = SymbolStringComparer.Instance.Compare(value2, value1);

            // Assert
            result.Should().BeGreaterThan(0);
        }
    }

    [Theory]
    [MemberData(nameof(TestData.StringArrays), MemberType = typeof(TestData))]
    public void Compare_StringArray_Order(string[] data)
    {
        // Arrange
        var expected = data.ToArray();
        Random.Shared.Shuffle(data); // Randomize test data

        // Act
        var results = data.Order(SymbolStringComparer.Instance).ToArray();

        // Assert
        results.Should().ContainInOrder(expected);

        if (!IsInvariantGlobalizationMode())
        {
            data.Order(StringComparer.InvariantCulture).Should().ContainInOrder(expected);
        }
    }

    [Fact]
    public void Compare_AsciiChars()
    {
        if (IsInvariantGlobalizationMode())
            Assert.Skip("This test needs `InvariantGlobalization:false` settings.");

        var asciiChars = Enumerable.Range(0, 128).Select(x => (char)x).ToArray();
        var allPairs = asciiChars.SelectMany(x => asciiChars, (x, y) => (xChar: x, yChar: y)).ToArray();

        foreach (var pair in allPairs)
        {
            var x = pair.xChar.ToString();
            var y = pair.yChar.ToString();

            // allPairs already enumerates every ordered pair, so a single Validate covers both directions.
            Validate(x, y);
        }
    }

    private static void Validate(string x, string y)
    {
        // Act
        var expected = Normalize(StringComparer.InvariantCulture.Compare(x, y));
        var actual = Normalize(SymbolStringComparer.Instance.Compare(x, y));

        // Assert
        actual.Should().Be(expected, $"xChar(U+{(x.Length > 0 ? ((int)x[0]).ToString("X4") : "empty")}) yChar(U+{(y.Length > 0 ? ((int)y[0]).ToString("X4") : "empty")})");
    }

    private static int Normalize(int value)
    {
        if (value == 0)
            return 0;
        if (value < 0)
            return -1;
        return 1;
    }

    private static bool IsInvariantGlobalizationMode()
    {
        return AppContext.TryGetSwitch("System.Globalization.Invariant", out bool isEnabled) && isEnabled;
    }
}
