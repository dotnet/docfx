// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownRewriters.Tests
{
    using System;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    public class MarkdownRewritersTest
    {
        [Fact]
        [Trait("Related", "MarkdownRewriters")]
        public void TestMarkdownRewriters_Simple()
        {
            var source = @"Hello world";
            var expected = @"Hello world

";
            var builder = new AzureEngineBuilder(new Options());
            var engine = builder.CreateEngine(new DfmMarkdownRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriters")]
        public void TestMarkdownRewriters_NormalBlockquote()
        {
            var source = @"> Hello world
this is new line originally  
> This is a new line";
            var expected = @"> Hello world
> this is new line originally  
> This is a new line
> 
> ";
            var builder = new AzureEngineBuilder(new Options());
            var engine = builder.CreateEngine(new DfmMarkdownRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriters")]
        public void TestMarkdownRewriters_NormalBlockquoteNest()
        {
            var source = @"> Hello world
this is new line originally  
> > This is a second nested first line
> > This is a second nested seconde line
> > > This is a third nested first line
> > This is a second nested second line
This is no-nested line
";
            var expected = @"> Hello world
> this is new line originally  
> 
> > This is a second nested first line
> > This is a second nested seconde line
> > 
> > > This is a third nested first line
> > > This is a second nested second line
> > > This is no-nested line
> > > 
> > > ";
            var builder = new AzureEngineBuilder(new Options());
            var engine = builder.CreateEngine(new DfmMarkdownRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriters")]
        public void TestMarkdownRewriters_AzureNoteBlockquote()
        {
            var source = @"> [AZURE.NOTE]
This is azure note
> [AZURE.WARNING]
This is azure warning";
            var expected = @"> [!NOTE]
> This is azure note
> 
> [!WARNING]
> This is azure warning
> 
> ";
            var builder = new AzureEngineBuilder(new Options());
            var engine = builder.CreateEngine(new DfmMarkdownRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriters")]
        public void TestMarkdownRewriters_AzureNoteBlockquoteNest()
        {
            var source = @"> [AZURE.NOTE]
This is azure note
> > [AZURE.WARNING]
This is azure warning
> > [AZURE.IMPORTANT]
> > This is azure Important
> > > [AZURE.TIP]
This is TIP
> > [AZURE.CAUTION]
> This is CAUTION";
            var expected = @"> [!NOTE]
> This is azure note
> 
> > [!WARNING]
> > This is azure warning
> > 
> > [!IMPORTANT]
> > This is azure Important
> > 
> > > [!TIP]
> > > This is TIP
> > > 
> > > [!CAUTION]
> > > This is CAUTION
> > > 
> > > ";
            var builder = new AzureEngineBuilder(new Options());
            var engine = builder.CreateEngine(new DfmMarkdownRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriters")]
        public void TestMarkdownRewriters_AzureInclude()
        {
            var source = @"This is azure include [AZURE.INCLUDE [include-short-name](../includes/include-file-name.md)] inline.

This is azure include block.

[AZURE.INCLUDE [include-short-name](../includes/include-file-name.md ""option title"")]";
            var expected = @"This is azure include [!INCLUDE [include-short-name](../includes/include-file-name.md)] inline.

This is azure include block.

[!INCLUDE [include-short-name](../includes/include-file-name.md ""option title"")]
";
            var builder = new AzureEngineBuilder(new Options());
            var engine = builder.CreateEngine(new DfmMarkdownRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        // [Fact]
        [Trait("Related", "MarkdownRewriters")]
        [Trait("Disable", "NotComplete")]
        public void TestMarkdownRewriters_ListWithAzureInclude()
        {
            var source = @"Hello world
* list [AZURE.INCLUDE [include-short-name](../includes/include-file-name.md)]
* list item2
- list item3
- list item4
---
1. nolist item1
2. nolist item2";
            var expected = @"> [!AZURE.NOTE]
> This is azure note
> > [!AZURE.WARNING]
> > This is azure warning
> > [!AZURE.IMPORTANT]
> > This is azure Important
> > > [!AZURE.TIP]
> > > This is TIP
> > > [!AZURE.CAUTION]
> > > This is CAUTION";
            var builder = new AzureEngineBuilder(new Options());
            var engine = builder.CreateEngine(new DfmMarkdownRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }
    }
}
