// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
    using System.Collections.Generic;
    using System.Composition.Hosting;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    [Collection("docfx STA")]
    public class DfmStaTest
    {
        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestTabGroup_DuplicateTabId()
        {
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter("test!!!!");
            var options = DocfxFlavoredMarked.CreateDefaultOptions();
            options.ShouldExportSourceInfo = true;
            var groupId = "uBn0rykxXo";
            string actual;
            try
            {
                Logger.RegisterListener(listener);
                using (new LoggerPhaseScope("test!!!!"))
                {
                    actual = DocfxFlavoredMarked.Markup(null, null, options, @"# [title-a](#tab/a)
content-a
# [title-a](#tab/a)
content-a
# <a id=""x""></a>[title-b](#tab/b/c)
content-b
- - -", "test.md");
                }
            }
            finally
            {
                Logger.UnregisterListener(listener);
            }

            var expected = $@"<div class=""tabGroup"" id=""tabgroup_{groupId}"" sourceFile=""test.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""7"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_{groupId}_a"" role=""tab"" aria-controls=""tabpanel_{groupId}_a"" data-tab=""a"" tabindex=""0"" aria-selected=""true"" sourceFile=""test.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""1"">title-a</a>
</li>
<li role=""presentation"">
<a href=""#tabpanel_{groupId}_a"" role=""tab"" aria-controls=""tabpanel_{groupId}_a"" data-tab=""a"" tabindex=""-1"" sourceFile=""test.md"" sourceStartLineNumber=""3"" sourceEndLineNumber=""3"">title-a</a>
</li>
<li role=""presentation"" aria-hidden=""true"" hidden=""hidden"">
<a href=""#tabpanel_{groupId}_b_c"" role=""tab"" aria-controls=""tabpanel_{groupId}_b_c"" data-tab=""b"" data-condition=""c"" tabindex=""-1"" sourceFile=""test.md"" sourceStartLineNumber=""5"" sourceEndLineNumber=""5"">title-b</a>
</li>
</ul>
<section id=""tabpanel_{groupId}_a"" role=""tabpanel"" data-tab=""a"">
<p sourceFile=""test.md"" sourceStartLineNumber=""2"" sourceEndLineNumber=""2"">content-a</p>
</section>
<section id=""tabpanel_{groupId}_a"" role=""tabpanel"" data-tab=""a"" aria-hidden=""true"" hidden=""hidden"">
<p sourceFile=""test.md"" sourceStartLineNumber=""4"" sourceEndLineNumber=""4"">content-a</p>
</section>
<section id=""tabpanel_{groupId}_b_c"" role=""tabpanel"" data-tab=""b"" data-condition=""c"" aria-hidden=""true"" hidden=""hidden"">
<p sourceFile=""test.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""6"">content-b</p>
</section>
</div>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), actual);
            Assert.Equal(new[] { WarningCodes.Markdown.DuplicateTabId }, from item in listener.Items select item.Code);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestTabGroup_DifferentTabIdSet()
        {
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter("test!!!!");
            var options = DocfxFlavoredMarked.CreateDefaultOptions();
            options.ShouldExportSourceInfo = true;
            var groupId = "uBn0rykxXo";
            string actual;
            try
            {
                Logger.RegisterListener(listener);
                using (new LoggerPhaseScope("test!!!!"))
                {
                    actual = DocfxFlavoredMarked.Markup(null, null, options, @"# [title-a](#tab/a)
content-a
# [title-b](#tab/b)
content-b
- - -
# [title-a](#tab/a)
content-a
# [title-c](#tab/c)
content-c
- - -", "test.md");
                }
            }
            finally
            {
                Logger.UnregisterListener(listener);
            }

            var expected = $@"<div class=""tabGroup"" id=""tabgroup_{groupId}"" sourceFile=""test.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""5"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_{groupId}_a"" role=""tab"" aria-controls=""tabpanel_{groupId}_a"" data-tab=""a"" tabindex=""0"" aria-selected=""true"" sourceFile=""test.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""1"">title-a</a>
</li>
<li role=""presentation"">
<a href=""#tabpanel_{groupId}_b"" role=""tab"" aria-controls=""tabpanel_{groupId}_b"" data-tab=""b"" tabindex=""-1"" sourceFile=""test.md"" sourceStartLineNumber=""3"" sourceEndLineNumber=""3"">title-b</a>
</li>
</ul>
<section id=""tabpanel_{groupId}_a"" role=""tabpanel"" data-tab=""a"">
<p sourceFile=""test.md"" sourceStartLineNumber=""2"" sourceEndLineNumber=""2"">content-a</p>
</section>
<section id=""tabpanel_{groupId}_b"" role=""tabpanel"" data-tab=""b"" aria-hidden=""true"" hidden=""hidden"">
<p sourceFile=""test.md"" sourceStartLineNumber=""4"" sourceEndLineNumber=""4"">content-b</p>
</section>
</div>
<div class=""tabGroup"" id=""tabgroup_{groupId}-1"" sourceFile=""test.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""10"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_{groupId}-1_a"" role=""tab"" aria-controls=""tabpanel_{groupId}-1_a"" data-tab=""a"" tabindex=""0"" aria-selected=""true"" sourceFile=""test.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""6"">title-a</a>
</li>
<li role=""presentation"">
<a href=""#tabpanel_{groupId}-1_c"" role=""tab"" aria-controls=""tabpanel_{groupId}-1_c"" data-tab=""c"" tabindex=""-1"" sourceFile=""test.md"" sourceStartLineNumber=""8"" sourceEndLineNumber=""8"">title-c</a>
</li>
</ul>
<section id=""tabpanel_{groupId}-1_a"" role=""tabpanel"" data-tab=""a"">
<p sourceFile=""test.md"" sourceStartLineNumber=""7"" sourceEndLineNumber=""7"">content-a</p>
</section>
<section id=""tabpanel_{groupId}-1_c"" role=""tabpanel"" data-tab=""c"" aria-hidden=""true"" hidden=""hidden"">
<p sourceFile=""test.md"" sourceStartLineNumber=""9"" sourceEndLineNumber=""9"">content-c</p>
</section>
</div>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), actual);
            Assert.Equal(new[] { WarningCodes.Markdown.DifferentTabIdSet }, from item in listener.Items select item.Code);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfm_InvalidYamlHeader_YamlUtilityThrowException()
        {
            var source = @"---
- Jon Schlinkert
- Brian Woodward

---

1
===
1.1
---
blabla

1.2
---
blabla";
            var expected = @"<hr/>
<ul>
<li>Jon Schlinkert</li>
<li>Brian Woodward</li>
</ul>
<hr/>
<h1 id=""1"">1</h1>
<h2 id=""11"">1.1</h2>
<p>blabla</p>
<h2 id=""12"">1.2</h2>
<p>blabla</p>
";

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter("YamlHeader");
            Logger.RegisterListener(listener);
            string marked;
            using (new LoggerPhaseScope("YamlHeader"))
            {
                marked = DocfxFlavoredMarked.Markup(source);
            }
            Logger.UnregisterListener(listener);

            Assert.Equal(1, listener.Items.Count(i => i.LogLevel == LogLevel.Warning));
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmTagValidate()
        {
            var builder = new DfmEngineBuilder(new Options() { Mangle = false });
            var mrb = new MarkdownValidatorBuilder(
                new CompositionContainer(
                    new ContainerConfiguration()
                        .WithAssembly(typeof(DfmTest).Assembly)
                        .CreateContainer()));
            mrb.AddTagValidators(new[]
            {
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "em", "div" },
                    MessageFormatter = "Invalid tag({0})!",
                    Behavior = TagValidationBehavior.Error,
                    OpeningTagOnly = true,
                },
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "h1" },
                    MessageFormatter = "Warning tag({0})!",
                    Behavior = TagValidationBehavior.Warning,
                },
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "script" },
                    MessageFormatter = "Warning tag({0})!",
                    Behavior = TagValidationBehavior.Warning,
                },
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "pre" },
                    MessageFormatter = "Warning tag({0})!",
                    Behavior = TagValidationBehavior.Warning,
                },
            });
            mrb.AddValidators(new[]
            {
                new MarkdownValidationRule
                {
                    ContractName =  HtmlMarkdownTokenValidatorProvider.ContractName,
                }
            });
            builder.Rewriter = mrb.CreateRewriter();

            var engine = builder.CreateDfmEngine(new DfmRenderer());
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter("test!!!!" + "." + MarkdownValidatorBuilder.MarkdownValidatePhaseName);
            Logger.RegisterListener(listener);
            string result;
            using (new LoggerPhaseScope("test!!!!"))
            {
                result = engine.Markup(@"<div><i>x</i><EM>y</EM><h1>z<pre><code>a*b*c</code></pre></h1></div>

<script>alert(1);</script>", "test");
            }
            Logger.UnregisterListener(listener);
            Assert.Equal(@"<div><i>x</i><EM>y</EM><h1>z<pre><code>a*b*c</code></pre></h1></div>

<script>alert(1);</script>".Replace("\r\n", "\n"), result);
            Assert.Equal(8, listener.Items.Count);
            Assert.Equal(new[]
            {
                HtmlMarkdownTokenValidatorProvider.WarningMessage,
                "Invalid tag(div)!",
                "Invalid tag(EM)!",
                "Warning tag(h1)!",
                "Warning tag(pre)!",
                "Warning tag(h1)!",
                "Html Tag!",
                "Warning tag(script)!",
            }, from item in listener.Items select item.Message);
        }
    }
}
