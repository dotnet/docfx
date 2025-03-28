// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Build.Engine;
using Docfx.DataContracts.Common;
using Docfx.MarkdigEngine;
using Docfx.Plugins;

namespace Docfx.Build.Common.Tests;

[TestClass]
public class ModelAttributeHandlerTest
{
    #region UniqueIdentityAttribute

    [TestMethod]
    public void TestSimpleModelWithUniqueIdentityReferenceAttributeShouldSucceed()
    {
        var model = new SimpleModel
        {
            Identity = "Identity1",
            Identities = ["Identity2"]
        };

        var context = Handle(model);

        Assert.AreEqual(2, context.LinkToUids.Count);
        Assert.Contains(model.Identity, context.LinkToUids);
        Assert.Contains(model.Identities[0], context.LinkToUids);
    }

    [TestMethod]
    public void TestModelWithLoopShouldSucceed()
    {
        var model = new LoopModel
        {
            Content = "Content1",
            Identity = "Identity1",
        };
        model.Reference = model;
        var context = Handle(model);
        Assert.ContainsSingle(context.LinkToUids);
    }

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

        Assert.AreEqual(12, context.LinkToUids.Count);
        CollectionAssert.AreEqual(new string[] {
            "0", "0.0", "0.0.0", "1", "1.1", "1.1.1","1.1.2", "1.2", "1.3","2",  "2.2", "3",
        }, context.LinkToUids.OrderBy(x => x).ToArray());
    }

    #endregion

    #region MarkdownContentAttribute

    [TestMethod]
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

        Assert.ContainsSingle(context.LinkToUids);
        Assert.ContainsSingle(context.LinkToFiles);
        Assert.ContainsSingle(context.FileLinkSources);
        Assert.ContainsSingle(context.UidLinkSources);
        Assert.AreEqual(
            @"<p sourcefile=""test"" sourcestartlinenumber=""1"">Hello <em sourcefile=""test"" sourcestartlinenumber=""1"">world</em>, <xref href=""xref"" data-throw-if-not-resolved=""False"" data-raw-source=""@xref"" sourcefile=""test"" sourcestartlinenumber=""1""></xref>, <a href=""link.md"" sourcefile=""test"" sourcestartlinenumber=""1"">link</a></p>
".Replace("\r\n", "\n"),
            model.Content);
    }

    [TestMethod]
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

        Assert.ContainsSingle(context.LinkToUids);
        Assert.ContainsSingle(context.LinkToFiles);
        Assert.ContainsSingle(context.FileLinkSources);
        Assert.ContainsSingle(context.UidLinkSources);
        var expected = @"<p sourcefile=""test"" sourcestartlinenumber=""1"">Hello <em sourcefile=""test"" sourcestartlinenumber=""1"">world</em>, <xref href=""xref"" data-throw-if-not-resolved=""False"" data-raw-source=""@xref"" sourcefile=""test"" sourcestartlinenumber=""1""></xref>, <a href=""link.md"" sourcefile=""test"" sourcestartlinenumber=""1"">link</a></p>
".Replace("\r\n", "\n");
        Assert.AreEqual(expected, model.Content);
        Assert.AreEqual(context.PlaceholderContent, model.Content2);
        Assert.AreEqual(context.PlaceholderContent, model.Inner.Content);
        Assert.AreEqual(context.PlaceholderContent, model.Inner.Content2);
        Assert.AreEqual(context.PlaceholderContent, model.Inner.Content3[0]);
        Assert.AreEqual(context.PlaceholderContent, model.Inner.Content4["key1"]);
        Assert.AreEqual(context.PlaceholderContent, model.Inner.Content5["key1"]);
    }

    [TestMethod]
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

        Assert.AreEqual(3, context.LinkToUids.Count);
        Assert.ContainsSingle(context.LinkToFiles);
        Assert.ContainsSingle(context.FileLinkSources);
        Assert.ContainsSingle(context.UidLinkSources);
        var expected = @"<p sourcefile=""test"" sourcestartlinenumber=""1"">Hello <em sourcefile=""test"" sourcestartlinenumber=""1"">world</em>, <xref href=""xref"" data-throw-if-not-resolved=""False"" data-raw-source=""@xref"" sourcefile=""test"" sourcestartlinenumber=""1""></xref>, <a href=""link.md"" sourcefile=""test"" sourcestartlinenumber=""1"">link</a></p>
".Replace("\r\n", "\n");
        Assert.AreEqual(expected, model.Content);
        Assert.AreEqual(context.PlaceholderContent, model.Content2);
        Assert.AreEqual("*content", model.Inner.Content);
        Assert.AreEqual("*content", model.Inner.Content2);
        Assert.AreEqual("Identity1", model.Inner.Content3[0]);
        Assert.AreEqual("*content", model.Inner.Content4["key1"]);
        Assert.AreEqual("*content", model.Inner.Content5["key1"]);
    }

    [TestMethod]
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

        Assert.ContainsSingle(context.LinkToUids);
        Assert.IsEmpty(context.LinkToFiles);
        Assert.IsEmpty(context.FileLinkSources);
        Assert.ContainsSingle(context.UidLinkSources);
        Assert.AreEqual("<p sourcefile=\"test\" sourcestartlinenumber=\"1\"><em sourcefile=\"test\" sourcestartlinenumber=\"1\">list</em></p>\n", model.ListContent[0]);
        Assert.AreEqual("<p sourcefile=\"test\" sourcestartlinenumber=\"1\"><xref href=\"xref\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@xref\" sourcefile=\"test\" sourcestartlinenumber=\"1\"></xref></p>\n", model.ArrayContent[0]);
        Assert.AreEqual("placeholder", model.ArrayContent[1]);
    }

    #endregion

    #region UrlContentAttribute

    [TestMethod]
    public void TestSimpleModelWithUrlContentAttributeShouldSucceed()
    {
        var model = new SimpleModel
        {
            Href = "linkTarget"
        };

        var context = Handle(model);

        Assert.IsEmpty(context.LinkToUids);
        Assert.IsEmpty(context.UidLinkSources);
        Assert.ContainsSingle(context.LinkToFiles);
        Assert.ContainsSingle(context.FileLinkSources);
        Assert.AreEqual("~/linkTarget", model.Href);
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
