// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.EntityMergers;
using Docfx.DataContracts.RestApi;
using Docfx.Exceptions;
using Xunit;

namespace Docfx.Build.RestApi.Tests;

public class RestApiViewModelTest
{
    [Fact]
    public void SchemaOverwritePreservesPositionalNullPlaceholders()
    {
        var schema = new RestApiSchemaViewModel
        {
            AllOf = [
                new() { Type = "object", Description = "First" },
                new() { Type = "object", Description = "Second" }]
        };
        var merger = new MergerFacade(new KeyedListMerger(new ReflectionEntityMerger()));
        merger.Merge(ref schema, new RestApiSchemaViewModel
        {
            AllOf = [null, new() { Description = "Updated" }]
        });
        Assert.Equal("First", schema.AllOf[0].Description);
        Assert.Equal("Updated", schema.AllOf[1].Description);
        Assert.Equal("object", schema.AllOf[1].Type);
        Assert.Throws<DocfxException>(() => merger.Merge(ref schema, new RestApiSchemaViewModel { AllOf = [] }));
    }

    [Fact]
    public void SplitDocumentContextIsIndependentAndPreservesOverrides()
    {
        var root = new RestApiRootItemViewModel
        {
            Info = new() { Title = "API", Description = "**API**" },
            SecurityDefinitions = new() { ["key"] = new() { Description = "**Key**" } },
            ExternalDocs = new() { Url = "https://example.test/root" }
        };
        var split = new RestApiRootItemViewModel
        {
            Metadata = new() { ["externalDocs"] = new { url = "https://example.test/tag" } }
        };
        root.CopyDocumentContextTo(split);
        Assert.Equal("https://example.test/tag", split.ExternalDocs.Url);
        Assert.Equal("https://example.test/root", root.ExternalDocs.Url);
        Assert.DoesNotContain("externalDocs", split.Metadata.Keys);
        split.Info.Description = "<p><strong>API</strong></p>";
        split.SecurityDefinitions["key"].Description = "<p><strong>Key</strong></p>";
        Assert.Equal("**API**", root.Info.Description);
        Assert.Equal("**Key**", root.SecurityDefinitions["key"].Description);
    }
}
