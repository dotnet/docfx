// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Build.Engine;
using Docfx.DataContracts.Common;
using Docfx.MarkdigEngine;
using Docfx.Plugins;
using Xunit;

namespace Docfx.Build.Common.Tests;

public class ModelAttributeHandlerTest
{
    #region UniqueIdentityAttribute

    [Fact]
    public void TestSimpleModelWithUniqueIdentityReferenceAttributeShouldSucceed()
    {
        var model = new SimpleModel
        {
            Identity = "Identity1",
            Identities = ["Identity2"]
        };

        var context = Handle(model);

        Assert.Equal(2, context.LinkToUids.Count);
        Assert.Contains(model.Identity, context.LinkToUids);
        Assert.Contains(model.Identities[0], context.LinkToUids);
    }

    [Fact]
    public void TestModelWithLoopShouldSucceed()
    {
        var model = new LoopModel
        {
            Content = "Content1",
            Identity = "Identity1",
        };
        model.Reference = model;
        var context = Handle(model);
        Assert.Single(context.LinkToUids);
    }

    [Fact]
    public void TestModelWithInvalidTypeShouldThrow()
    {
        var model = new InvalidModel
        {
            Identity = "identity",
            InvalidIdentity = 1,
        };
        Assert.Throws<NotSupportedException>(
            () => Handle(model)
            );
    }

    [Fact]
    public void TestModelWithInvalidItemTypeShouldThrow()
    {
        var model = new InvalidModel2
        {
            Identities = [0]
        };
        Assert.Throws<NotSupportedException>(
            () => Handle(model)
            );
    }

    [Fact]
    public void TestComplexModelWithUniqueIdentityReferenceAttributeShouldSucceed()
    {
        var model = new ComplexModel
        {
            Identities = ["1", "2", "3"],
            Identity = "0",
            Inner = new ComplexModel
            {
                Identities = ["1.1", "1.2", "1.3"],
                Identity = "0.0",
                OtherProperty = "innerothers",
                Inner = new ComplexModel
                {
                    Identities = ["1.1.1", "1.1.2"],
                    Identity = "0.0.0",
                    OtherProperty = "innersinner"
                }
            },
            OtherProperty = "others",
            InnerModels =
            [
                new()
                {
                     Identity = "2.1",
                     CrefType = TestCrefType.Cref
                },
                new()
                {
                     Identity = "2.2",
                     CrefType = TestCrefType.Href
                }
            ]
        };
        var context = Handle(model);

        Assert.Equal(12, context.LinkToUids.Count);
        Assert.Equal(new List<string> {
            "0", "0.0", "0.0.0", "1", "1.1", "1.1.1","1.1.2", "1.2", "1.3","2",  "2.2", "3",
        }, context.LinkToUids.OrderBy(x => x));
    }

    #endregion

    #region MarkdownContentAttribute

    [Fact]
    public void TestSimpleModelWithMarkdownContentAttributeShouldSucceed()
    {
        var model = new MarkdownModel1
        {
            Content = "Hello *world*, @xref, [link](link.md)",
            Content2 = "Content2",
            Content3 =
             [
                 "Content3",
                 "Content3.1"
             ]
        };

        var context = Handle(model);

        Assert.Single(context.LinkToUids);
        Assert.Single(context.LinkToFiles);
        Assert.Single(context.FileLinkSources);
        Assert.Single(context.UidLinkSources);
        Assert.Equal(
            @"<p sourcefile=""test"" sourcestartlinenumber=""1"">Hello <em sourcefile=""test"" sourcestartlinenumber=""1"">world</em>, <xref href=""xref"" data-throw-if-not-resolved=""False"" data-raw-source=""@xref"" sourcefile=""test"" sourcestartlinenumber=""1""></xref>, <a href=""link.md"" sourcefile=""test"" sourcestartlinenumber=""1"">link</a></p>
".Replace("\r\n", "\n"),
            model.Content);
    }

    [Fact]
    public void TestMarkdownContentAttributeWithContentPlaceholderShouldSucceed()
    {
        var model = new MarkdownModel1
        {
            Content = "Hello *world*, @xref, [link](link.md)",
            Content2 = "*content",
            Inner = new MarkdownModel1
            {
                Content = "*content",
                Content2 = "*content",
                Content3 =
                [
                    "*content"
                ],
                Content4 = new Dictionary<string, object>
                {
                    ["key1"] = "*content"
                },
                Content5 = new SortedList<string, object>
                {
                    ["key1"] = "*content"
                }
            }
        };

        var context = GetDefaultContext();
        context.EnableContentPlaceholder = true;
        context.PlaceholderContent = "placeholder";
        context = Handle(model, context);

        Assert.Single(context.LinkToUids);
        Assert.Single(context.LinkToFiles);
        Assert.Single(context.FileLinkSources);
        Assert.Single(context.UidLinkSources);
        var expected = @"<p sourcefile=""test"" sourcestartlinenumber=""1"">Hello <em sourcefile=""test"" sourcestartlinenumber=""1"">world</em>, <xref href=""xref"" data-throw-if-not-resolved=""False"" data-raw-source=""@xref"" sourcefile=""test"" sourcestartlinenumber=""1""></xref>, <a href=""link.md"" sourcefile=""test"" sourcestartlinenumber=""1"">link</a></p>
".Replace("\r\n", "\n");
        Assert.Equal(expected, model.Content);
        Assert.Equal(context.PlaceholderContent, model.Content2);
        Assert.Equal(context.PlaceholderContent, model.Inner.Content);
        Assert.Equal(context.PlaceholderContent, model.Inner.Content2);
        Assert.Equal(context.PlaceholderContent, model.Inner.Content3[0]);
        Assert.Equal(context.PlaceholderContent, model.Inner.Content4["key1"]);
        Assert.Equal(context.PlaceholderContent, model.Inner.Content5["key1"]);
    }

    [Fact]
    public void TestMarkdownContentIgnoreAttributeShouldSucceed()
    {
        var model = new MarkdownModelWithIgnore
        {
            Content = "Hello *world*, @xref, [link](link.md)",
            Content2 = "*content",
            Inner = new MarkdownModelWithIgnore
            {
                Content = "*content",
                Content2 = "*content",
                Content3 =
                [
                    "Identity1",
                    "Identity2"
                ],
                Content4 = new Dictionary<string, object>
                {
                    ["key1"] = "*content"
                },
                Content5 = new SortedList<string, object>
                {
                    ["key1"] = "*content"
                }
            }
        };

        var context = GetDefaultContext();
        context.EnableContentPlaceholder = true;
        context.PlaceholderContent = "placeholder";
        context = Handle(model, context);

        Assert.Equal(3, context.LinkToUids.Count);
        Assert.Single(context.LinkToFiles);
        Assert.Single(context.FileLinkSources);
        Assert.Single(context.UidLinkSources);
        var expected = @"<p sourcefile=""test"" sourcestartlinenumber=""1"">Hello <em sourcefile=""test"" sourcestartlinenumber=""1"">world</em>, <xref href=""xref"" data-throw-if-not-resolved=""False"" data-raw-source=""@xref"" sourcefile=""test"" sourcestartlinenumber=""1""></xref>, <a href=""link.md"" sourcefile=""test"" sourcestartlinenumber=""1"">link</a></p>
".Replace("\r\n", "\n");
        Assert.Equal(expected, model.Content);
        Assert.Equal(context.PlaceholderContent, model.Content2);
        Assert.Equal("*content", model.Inner.Content);
        Assert.Equal("*content", model.Inner.Content2);
        Assert.Equal("Identity1", model.Inner.Content3[0]);
        Assert.Equal("*content", model.Inner.Content4["key1"]);
        Assert.Equal("*content", model.Inner.Content5["key1"]);
    }

    [Fact]
    public void TesteModelWithIListMarkdownContentAttributeShouldSucceed()
    {
        var model = new MarkdownModelWithIList
        {
            ListContent = ["*list*"],
            ArrayContent = ["@xref", "*content"]
        };

        var context = GetDefaultContext();
        context.EnableContentPlaceholder = true;
        context.PlaceholderContent = "placeholder";
        context = Handle(model, context);

        Assert.Single(context.LinkToUids);
        Assert.Empty(context.LinkToFiles);
        Assert.Empty(context.FileLinkSources);
        Assert.Single(context.UidLinkSources);
        Assert.Equal("<p sourcefile=\"test\" sourcestartlinenumber=\"1\"><em sourcefile=\"test\" sourcestartlinenumber=\"1\">list</em></p>\n", model.ListContent[0]);
        Assert.Equal("<p sourcefile=\"test\" sourcestartlinenumber=\"1\"><xref href=\"xref\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@xref\" sourcefile=\"test\" sourcestartlinenumber=\"1\"></xref></p>\n", model.ArrayContent[0]);
        Assert.Equal("placeholder", model.ArrayContent[1]);
    }

    #endregion

    #region UrlContentAttribute

    [Fact]
    public void TestSimpleModelWithUrlContentAttributeShouldSucceed()
    {
        var model = new SimpleModel
        {
            Href = "linkTarget"
        };

        var context = Handle(model);

        Assert.Empty(context.LinkToUids);
        Assert.Empty(context.UidLinkSources);
        Assert.Single(context.LinkToFiles);
        Assert.Single(context.FileLinkSources);
        Assert.Equal("~/linkTarget", model.Href);
    }
    #endregion

    #region Helper Method

    private static HandleModelAttributesContext Handle(object model, HandleModelAttributesContext context = null)
    {
        var handler = new CompositeModelAttributeHandler(
            new UniqueIdentityReferenceHandler(),
            new MarkdownContentHandler(),
            new UrlContentHandler());
        context ??= GetDefaultContext();
        handler.Handle(model, context);
        return context;
    }

    private static HandleModelAttributesContext GetDefaultContext()
    {
        return new HandleModelAttributesContext
        {
            Host = new HostService([])
            {
                MarkdownService = new MarkdigMarkdownService(new MarkdownServiceParameters { BasePath = string.Empty }),
                SourceFiles = new Dictionary<string, FileAndType>
                {
                    { "~/test" , new FileAndType(Environment.CurrentDirectory, "test", DocumentType.Article)},
                    { "~/linkTarget" , new FileAndType(Environment.CurrentDirectory, "linkTarget", DocumentType.Article)}
                }.ToImmutableDictionary()
            },
            FileAndType = new FileAndType(Environment.CurrentDirectory, "test", DocumentType.Article),
        };
    }

    #endregion

    #region Test Data

    public class LoopModel
    {
        [MarkdownContent]
        public string Content { get; set; }

        [UniqueIdentityReference]
        public string Identity { get; set; }

        public LoopModel Reference { get; set; }
    }

    public class MarkdownModel1
    {
        [MarkdownContent]
        public string Content { get; set; }

        public string Content2 { get; set; }

        [MarkdownContent]
        public string ReadonlyContent { get; } = "*content";

        [UniqueIdentityReference]
        [UniqueIdentityReferenceIgnore]
        public string[] Content3 { get; set; }

        public MarkdownModel1 Inner { get; set; }

        public Dictionary<string, object> Content4 { get; set; }

        public SortedList<string, object> Content5 { get; set; }
    }

    public class MarkdownModelWithIgnore
    {
        [MarkdownContent]
        public string Content { get; set; }

        [MarkdownContent]
        public string Content2 { get; set; }

        [MarkdownContent]
        public string ReadonlyContent { get; } = "*content";

        [UniqueIdentityReference]
        public string[] Content3 { get; set; }

        [MarkdownContentIgnore]
        public MarkdownModelWithIgnore Inner { get; set; }

        [MarkdownContentIgnore]
        public Dictionary<string, object> Content4 { get; set; }

        [MarkdownContentIgnore]
        public SortedList<string, object> Content5 { get; set; }
    }

    public class MarkdownModelWithIList
    {
        [MarkdownContent]
        public List<string> ListContent { get; set; }

        [MarkdownContent]
        public string[] ArrayContent { get; set; }
    }

    public class SimpleModel
    {
        [UniqueIdentityReference]
        public string Identity { get; set; }
        [UniqueIdentityReference]
        public List<object> Identities { get; set; }
        [UrlContent]
        public string Href { get; set; }
    }

    public class InvalidModel
    {
        [UniqueIdentityReference]
        public int InvalidIdentity { get; set; }

        [UniqueIdentityReference]
        public string Identity { get; set; }
    }

    public class InvalidModel2
    {
        [UniqueIdentityReference]
        public List<int> Identities { get; set; }
    }

    public class ComplexModel
    {
        [UniqueIdentityReference]
        public string Identity { get; set; }

        [UniqueIdentityReference]
        public List<string> Identities { get; set; }

        [UniqueIdentityReference]
        public IEnumerable<string> Substitute => InnerModels?.Where(s => s.CrefType == TestCrefType.Href).Select(s => s.Identity);

        public List<InnerModel> InnerModels { get; set; }

        public ComplexModel Inner { get; set; }

        public string OtherProperty { get; set; }
    }

    public class InnerModel
    {
        public string Identity { get; set; }
        public TestCrefType CrefType { get; set; }
    }

    public enum TestCrefType
    {
        Href,
        Cref
    }

    #endregion
}
