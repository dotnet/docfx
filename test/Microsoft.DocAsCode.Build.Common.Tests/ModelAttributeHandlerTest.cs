// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    [Trait("Owner", "lianwei")]
    public class ModelAttributeHandlerTest
    {
        #region UniqueIdentityAttribute

        [Fact]
        public void TestSimpleModelWithUniqueIdentityReferenceAttributeShouldSucceed()
        {
            var model = new SimpleModel
            {
                Identity = "Identity1",
                Identities = new List<object> { "Identity2" }
            };

            var context = Handle(model);

            Assert.Equal(2, context.LinkToUids.Count);
            Assert.True(context.LinkToUids.Contains(model.Identity));
            Assert.True(context.LinkToUids.Contains(model.Identities[0]));
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
            Assert.Equal(1, context.LinkToUids.Count);
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
                Identities = new List<int> { 0 }
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
                Identities = new List<string> { "1", "2", "3" },
                Identity = "0",
                Inner = new ComplexModel
                {
                    Identities = new List<string> { "1.1", "1.2", "1.3" },
                    Identity = "0.0",
                    OtherProperty = "innerothers",
                    Inner = new ComplexModel
                    {
                        Identities = new List<string> { "1.1.1", "1.1.2" },
                        Identity = "0.0.0",
                        OtherProperty = "innersinner"
                    }
                },
                OtherProperty = "others",
                InnerModels = new List<InnerModel>
                {
                    new InnerModel
                    {
                         Identity = "2.1",
                         CrefType = TestCrefType.Cref
                    },
                    new InnerModel
                    {
                         Identity = "2.2",
                         CrefType = TestCrefType.Href
                    }
                }
            };
            var context = Handle(model);

            Assert.Equal(12, context.LinkToUids.Count);
            Assert.Equal(new List<string> {
                "0", "1", "2", "3", "2.2", "0.0", "1.1", "1.2", "1.3", "0.0.0", "1.1.1", "1.1.2"
            }, context.LinkToUids);
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
                Content3 = new string[]
                 {
                     "Content3",
                     "Content3.1"
                 }
            };

            var context = Handle(model);

            Assert.Equal(1, context.LinkToUids.Count);
            Assert.Equal(1, context.LinkToFiles.Count);
            Assert.Equal(1, context.FileLinkSources.Count);
            Assert.Equal(1, context.UidLinkSources.Count);
            Assert.Equal(
                @"<p sourcefile=""test"" sourcestartlinenumber=""1"" sourceendlinenumber=""1"">Hello <em>world</em>, <xref href=""xref"" data-throw-if-not-resolved=""False"" data-raw-source=""@xref"" sourcefile=""test"" sourcestartlinenumber=""1"" sourceendlinenumber=""1""></xref>, <a href=""link.md"" data-raw-source=""[link](link.md)"" sourcefile=""test"" sourcestartlinenumber=""1"" sourceendlinenumber=""1"">link</a></p>
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
                    Content3 = new string[]
                    {
                        "*content"
                    },
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

            Assert.Equal(1, context.LinkToUids.Count);
            Assert.Equal(1, context.LinkToFiles.Count);
            Assert.Equal(1, context.FileLinkSources.Count);
            Assert.Equal(1, context.UidLinkSources.Count);
            var expected = @"<p sourcefile=""test"" sourcestartlinenumber=""1"" sourceendlinenumber=""1"">Hello <em>world</em>, <xref href=""xref"" data-throw-if-not-resolved=""False"" data-raw-source=""@xref"" sourcefile=""test"" sourcestartlinenumber=""1"" sourceendlinenumber=""1""></xref>, <a href=""link.md"" data-raw-source=""[link](link.md)"" sourcefile=""test"" sourcestartlinenumber=""1"" sourceendlinenumber=""1"">link</a></p>
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
                    Content3 = new string[]
                    {
                        "Identity1",
                        "Identity2"
                    },
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
            Assert.Equal(1, context.LinkToFiles.Count);
            Assert.Equal(1, context.FileLinkSources.Count);
            Assert.Equal(1, context.UidLinkSources.Count);
            var expected = @"<p sourcefile=""test"" sourcestartlinenumber=""1"" sourceendlinenumber=""1"">Hello <em>world</em>, <xref href=""xref"" data-throw-if-not-resolved=""False"" data-raw-source=""@xref"" sourcefile=""test"" sourcestartlinenumber=""1"" sourceendlinenumber=""1""></xref>, <a href=""link.md"" data-raw-source=""[link](link.md)"" sourcefile=""test"" sourcestartlinenumber=""1"" sourceendlinenumber=""1"">link</a></p>
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
                ListContent = new List<string> { "*list*" },
                ArrayContent = new[] { "@xref", "*content" }
            };

            var context = GetDefaultContext();
            context.EnableContentPlaceholder = true;
            context.PlaceholderContent = "placeholder";
            context = Handle(model, context);

            Assert.Equal(1, context.LinkToUids.Count);
            Assert.Equal(0, context.LinkToFiles.Count);
            Assert.Equal(0, context.FileLinkSources.Count);
            Assert.Equal(1, context.UidLinkSources.Count);
            Assert.Equal("<p sourcefile=\"test\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\"><em>list</em></p>\n", model.ListContent[0]);
            Assert.Equal("<p sourcefile=\"test\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\"><xref href=\"xref\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@xref\" sourcefile=\"test\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\"></xref></p>\n", model.ArrayContent[0]);
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

            Assert.Equal(0, context.LinkToUids.Count);
            Assert.Equal(0, context.UidLinkSources.Count);
            Assert.Equal(1, context.LinkToFiles.Count);
            Assert.Equal(1, context.FileLinkSources.Count);
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
            if (context == null)
            {
                context = GetDefaultContext();
            }
            handler.Handle(model, context);
            return context;
        }

        private static HandleModelAttributesContext GetDefaultContext()
        {
            return new HandleModelAttributesContext
            {
                Host = new HostService(null, Enumerable.Empty<FileModel>())
                {
                    MarkdownService = new DfmServiceProvider().CreateMarkdownService(new MarkdownServiceParameters { BasePath = string.Empty }),
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
}
