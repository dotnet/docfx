// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

#nullable enable

namespace Docfx.Dotnet.Tests;

public partial class SymbolStringComparerTest
{
    private static class TestData
    {
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
