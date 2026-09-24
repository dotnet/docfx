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
}
