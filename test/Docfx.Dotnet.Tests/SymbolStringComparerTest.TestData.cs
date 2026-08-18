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
        public static TheoryData<string[]> StringArrays =>
        [
            // Contains underscore
            [
                "__",
                "__a",
                "_1",
                "1_",
                "a_a",
                "A_a",
                "a_aa",
                "a_ab",
                "aaa"
            ],
            // Case differences
            [
                "aaa",
                "AAA",
                "AAA<ABC>",
                "AAAA",
                "aaab",
            ],
            // Mixed generics
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
            ],
        ];
    }
}
