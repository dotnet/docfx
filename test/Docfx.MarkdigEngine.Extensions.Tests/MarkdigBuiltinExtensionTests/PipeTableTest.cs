// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx.MarkdigEngine.Extensions;
using Markdig.Extensions.Tables;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

/// <summary>
/// Unit tests for markdig <see cref="PipeTableExtension"/>.
/// </summary>
/// <seealso href="https://github.com/xoofx/markdig/blob/master/src/Markdig.Tests/Specs/PipeTableSpecs.md"/>
[Trait("Related", "MarkdigExtension")]
public class PipeTableTest
{
    [Fact]
    public void PipeTableTest_Default()
    {
        var content =
            """
            a | b
            -- | -
            0 | 1 | 2
            """;

        var expected =
            """
            <table>
            <thead>
            <tr>
            <th>a</th>
            <th>b</th>
            <th></th>
            </tr>
            </thead>
            <tbody>
            <tr>
            <td>0</td>
            <td>1</td>
            <td>2</td>
            </tr>
            </tbody>
            </table>
            """;

        TestUtility.VerifyMarkup(content, expected);
        TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["PipeTables"]);
    }

    [Fact]
    public void PipeTableTest_Custom()
    {
        var options = new PipeTableOptions
        {
            RequireHeaderSeparator = true,  // Defaut: true
            UseHeaderForColumnCount = true, // Default: false
        };

        var content =
            """
            a | b
            -- | -
            0 | 1 | IgnoredColumn
            """;

        var expected =
            """
            <table>
            <thead>
            <tr>
            <th>a</th>
            <th>b</th>
            </tr>
            </thead>
            <tbody>
            <tr>
            <td>0</td>
            <td>1</td>
            </tr>
            </tbody>
            </table>
            """;

        TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["gfm-pipetables"]);
        TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
            new ("PipeTables", JsonSerializer.SerializeToNode(options,  MarkdigExtensionSettingConverter.DefaultSerializerOptions))
        ]);
    }
}
