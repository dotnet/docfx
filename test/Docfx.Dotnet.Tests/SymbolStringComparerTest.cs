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

        if (!IsInvariantGlobalizationMode())
        {
            var result2 = StringComparer.InvariantCulture.Compare(value1, value2);
            result2.Should().Be(0);
        }
    }

    [Theory]
    // Punctual-> Number -> Alphabet
    [InlineData("_", "a")]
    [InlineData("0", "a")]
    // lower-case alphabet is ordered before upper-case.
    [InlineData("a", "A")]
    [InlineData("z", "Z")]
    [InlineData("a", "Z")]
    [InlineData("test", "TEST")]
    // Casing
    [InlineData("aa", "aA")]
    [InlineData("aa", "ab")]
    [InlineData("aA", "aB")]
    [InlineData("aA", "Ab")]
    [InlineData("abc", "ABC")]    // Lowercase before uppercase
    [InlineData("aBC", "AbC")]    // Uppercase after lowercase
    [InlineData("AAAA", "abcd")]  // Compare `A` with `b` (Case diffs are ignored)
    [InlineData("AAAA", "aaaab")] // Compare length (Case diffs are ignored)
    [InlineData("abc", "abcd")]   // Compare length diff
    // Underscore prefix/suffix
    [InlineData("_test", "test")]  // Compare `_/` with `t`
    [InlineData("__a", "_1")]      // Compare `_` with `1`
    [InlineData("a_b", "a_c")]     // Compare `b` with `c`
    [InlineData("test_", "testz")] // Compare `_` with `z`
    [InlineData("a_a", "a_b")]     // Compare `a` with `b`
    [InlineData("a_aa", "aa_a")]   // Compare `_` with `a`
    [InlineData("test", "test_")]  // Compare length diff
    [InlineData("A_a", "a_aaa")]   // Compare length diff
    [InlineData("a_abc", "a_ABC")] // Compare case diff (if text has same length)
    // Generics
    [InlineData("List", "List<T>")]
    [InlineData("List<int>", "List<string>")]
    // Punctual
    [InlineData("<", "a")]
    [InlineData("!", "a")]
    [InlineData("_", "`")]
    // Null
    [InlineData(null, "test")]
    // Non-ASCII char
    [InlineData("hello①", "hello②")]
    // Overload
    [InlineData("Contains(Char)", "Contains(Char, StringComparison)")]
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

        if (!IsInvariantGlobalizationMode())
        {
            var result2 = StringComparer.InvariantCulture.Compare(value1, value2);
            result2.Should().BeLessThan(0);
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
        {
            // TODO: Enable following line after migrated to xUnit.v3
            // Assert.Skip("This test needs `InvariantGlobalization:false` settings.");
            return;
        }

        var asciiChars = Enumerable.Range(0, 128).Select(x => (char)x).ToArray();
        var allPairs = asciiChars.SelectMany(x => asciiChars, (x, y) => (xChar: x, yChar: y)).ToArray();

        foreach (var pair in allPairs)
        {
            var x = pair.xChar.ToString();
            var y = pair.yChar.ToString();

            var expected = Normalize(StringComparer.InvariantCulture.Compare(x, y));
            var actual = Normalize(SymbolStringComparer.Instance.Compare(x, y));

            // Handle custom logics that is not compatible to StringComparer.InvariantCulture
            if ((pair.xChar == ',' && pair.yChar == ')') || (pair.xChar == ')' && pair.yChar == ','))
                actual = -actual;

            actual.Should().Be(expected);
        }

        static int Normalize(int value)
        {
            if (value == 0)
                return 0;
            if (value < 0)
                return -1;
            return 1;
        }
    }

    private static bool IsInvariantGlobalizationMode()
    {
        return AppContext.TryGetSwitch("System.Globalization.Invariant", out bool isEnabled) && isEnabled;
    }
}
