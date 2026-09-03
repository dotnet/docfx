// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Docfx.Common;
using Docfx.DataContracts.Common;
using FluentAssertions;
using Xunit;

namespace Docfx.Build.TableOfContents.Tests;

public class TocHelperTest
{
    [Fact]
    public void TestItemDeserialization()
    {
        // Arrange
        var item = new TocItemViewModel
        {
            Items =
            [
                new TocItemViewModel { Uid = "item1" },
                new TocItemViewModel { Uid = "item2" }
            ],
        };

        var yaml = ToYaml(item);
        var filePath = Path.Combine(Path.GetTempPath(), "toc.yml");
        File.WriteAllText(filePath, yaml, new UTF8Encoding(false));

        try
        {
            // Act
            var result = TocHelper.LoadSingleToc(filePath);

            // Assert
            result.Should().BeEquivalentTo(item);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void TestListDeserialization()
    {
        // Arrange
        var items = new TocItemViewModel[]
        {
            new TocItemViewModel { Uid = "item1" },
            new TocItemViewModel { Uid = "item2" },
        };

        var yaml = ToYaml(items);
        var filePath = Path.Combine(Path.GetTempPath(), "toc.yml");
        File.WriteAllText(filePath, yaml);

        try
        {
            // Act
            var result = TocHelper.LoadSingleToc(filePath);

            // Assert
            result.Uid.Should().BeNull();
            result.Href.Should().BeNull();
            result.Items.Should().BeEquivalentTo(items);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void TestItemDeserializationWithEncoding()
    {
        // Arrange
        var item = new TocItemViewModel
        {
            Items =
            [
                new TocItemViewModel { Uid = "item1" },
                new TocItemViewModel { Uid = "item2" }
            ],
        };

        var yaml = ToYaml(item);

        foreach (var encoding in Encodings)
        {
            var filePath = Path.Combine(Path.GetTempPath(), "toc.yml");
            File.WriteAllText(filePath, yaml, encoding);

            try
            {
                // Act
                var result = TocHelper.LoadSingleToc(filePath);

                // Assert
                result.Should().BeEquivalentTo(item);
            }
            finally
            {
                File.Delete(filePath);
            }
        }
    }

    private static readonly Encoding[] Encodings =
    [
        new UTF8Encoding(false),
        new UTF8Encoding(true),
        Encoding.Unicode,
        Encoding.BigEndianUnicode,
    ];

    private static string ToYaml<T>(T model)
    {
        using StringWriter sw = new StringWriter();
        YamlUtility.Serialize(sw, model);
        return sw.ToString();
    }
}
