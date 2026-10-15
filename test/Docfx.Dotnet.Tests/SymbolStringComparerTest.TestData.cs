// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

#nullable enable

namespace Docfx.Dotnet.Tests;

public partial class SymbolStringComparerTest
{
    private static class TestData
    {
        public static TheoryData<string?, string?> StringPatterns =>
        [
            // Punctual-> Number -> Alphabet
            ("_", "a"),
            ("0", "a"),
            // lower-case alphabet is ordered before upper-case.
            ("a", "A"),
            ("z", "Z"),
            ("a", "Z"),
            ("test", "TEST"),
            // Casing
            ("aa", "aA"),
            ("aa", "ab"),
            ("aA", "aB"),
            ("aA", "Ab"),
            ("abc", "ABC"),    // Lowercase before uppercase
            ("aBC", "AbC"),    // Uppercase after lowercase
            ("AAAA", "abcd"),  // Compare `A` with `b` (Case diffs are ignored)
            ("AAAA", "aaaab"), // Compare length (Case diffs are ignored)
            ("abc", "abcd"),   // Compare length diff
            // Underscore prefix/suffix
            ("_test", "test"),  // Compare `_/` with `t`
            ("__a", "_1"),      // Compare `_` with `1`
            ("a_b", "a_c"),     // Compare `b` with `c`
            ("test_", "testz"), // Compare `_` with `z`
            ("a_a", "a_b"),     // Compare `a` with `b`
            ("a_aa", "aa_a"),   // Compare `_` with `a`
            ("test", "test_"),  // Compare length diff
            ("A_a", "a_aaa"),   // Compare length diff
            ("a_abc", "a_ABC"), // Compare case diff (if text has same length)
            // Generics
            ("List", "List<T>"),
            ("List<int>", "List<string>"),
            // Punctual
            ("<", "a"),
            ("!", "a"),
            ("_", "`"),
            // Null
            (null, "test"),
            // Non-ASCII char
            ("hello①", "hello②"),
            // Overload: a trailing ')' (end of parameter list) sorts before ',' and '.'.
            // (Global single-char order is ',' < '.' < ')'; the overload rule applies only inside parameter lists.)
            ("Contains(Char)", "Contains(Char, StringComparison)"),
            ("M(A)", "M(A,B)"),
            ("M(A)", "M(A.B)"),
            ("M(A,B)", "M(A.B)"),
        ];

        /// <summary>
        /// Test data for string array order tests.
        /// </summary>
        public static IEnumerable<TheoryDataRow<string[]>> StringArrays =>
        [
            // Contains underscore
            new TheoryDataRow<string[]>([
                "__",
                "__a",
                "_1",
                "1_",
                "a_a",
                "A_a",
                "a_aa",
                "a_ab",
                "aaa",
            ]),
            // Case differences
            new TheoryDataRow<string[]>(
            [
                "aaa",
                "AAA",
                "AAA<ABC>",
                "AAAA",
                "aaab",
            ]),
            // Mixed generics
            new TheoryDataRow<string[]>(
            [
                "IRoutedView",
                "IRoutedView_`1",
                "IRoutedView`1",
                "IRoutedView<TViewModel>",
                "IRoutedView1",
                "IRoutedViewModel",
                "Null(object? obj)",
                "Null<T>(T obj)",
                "NullOrEmpty(string? text)",
            ]),
        ];
    }
}
