// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Composition.Hosting;
    using System.Linq;
    using System.IO;

    using MarkdigEngine.Extensions;

    using Markdig;
    using Markdig.Syntax;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdigEngine.Validators;
    using Microsoft.DocAsCode.Plugins;
    using Xunit;

    public class ValidationTest
    {
        public const string MarkdownValidatePhaseName = "Markdown style";

        private readonly MarkdownContext DefaultContext = 
            new MarkdownContext(
                null,
                (code, message, origin, line) => Logger.LogInfo(message, null, null, line.ToString(), code),
                (code, message, origin, line) => Logger.LogSuggestion(message, null, null, line.ToString(), code),
                (code, message, origin, line) => Logger.LogWarning(message, null, null, line.ToString(), code),
                (code, message, origin, line) => Logger.LogError(message, null, null, line.ToString(), code));

        [Fact]
        [Trait("Related", "Validation")]
        public void TestHtmlBlockTagValidation()
        {
            var content = @"
<div class='a'>
    <i>x</i>
    <EM>y</EM>
    <h1>
        z
        <pre><code>
            a*b*c
        </code></pre>
    </h1>
</div>
<script>alert(1);</script>";

            var builder = MarkdownValidatorBuilder.Create(
                null,
                new CompositionContainer(
                    new ContainerConfiguration()
                        .WithAssembly(typeof(ValidationTest).Assembly)
                        .CreateContainer()));

            builder.AddTagValidators(new[]
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
                    OpeningTagOnly = true
                },
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "pre" },
                    MessageFormatter = "Warning tag({0})!",
                    Behavior = TagValidationBehavior.Warning,
                }
            });

            builder.AddValidators(new[]
            {
                new MarkdownValidationRule
                {
                    ContractName =  HtmlMarkdownObjectValidatorProvider.ContractName,
                }
            });

            builder.LoadEnabledRulesProvider();
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(MarkdownValidatePhaseName);
            using (new LoggerPhaseScope(MarkdownValidatePhaseName))
            {
                var html = Markup(content, builder.CreateRewriter(DefaultContext), listener);

                Assert.Equal(@"<div class='a'>
    <i>x</i>
    <EM>y</EM>
    <h1>
        z
        <pre><code>
            a*b*c
        </code></pre>
    </h1>
</div>
<script>alert(1);</script>
".Replace("\r\n", "\n"), html);
            }
            Assert.Equal(8, listener.Items.Count);
            Assert.Equal(new[]
            {
                "Html Tag!",
                "Invalid tag(div)!",
                "Invalid tag(EM)!",
                "Warning tag(h1)!",
                "Warning tag(pre)!",
                "Warning tag(pre)!",
                "Warning tag(h1)!",
                "Warning tag(script)!"
            }, from item in listener.Items select item.Message);
        }

        [Fact]
        [Trait("Related", "Validation")]
        public void TestHtmlBlockTagNotInRelationValidation()
        {
            var content = @"
<div class='a'>
    <i>x</i>
    <EM>y</EM>
    <h1>
        z
        <pre><code>
            a*b*c
        </code></pre>
    </h1>
</div>
<script>alert(1);</script>";

            var builder = MarkdownValidatorBuilder.Create(null, null);
            builder.AddTagValidators(new[]
            {
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "h1", "code", "pre", "div" },
                    MessageFormatter = "Invalid tag({0})!",
                    Behavior = TagValidationBehavior.Error,
                    OpeningTagOnly = true,
                    Relation = TagRelation.NotIn
                }
            });

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(MarkdownValidatePhaseName);
            using (new LoggerPhaseScope(MarkdownValidatePhaseName))
            {
                var html = Markup(content, builder.CreateRewriter(DefaultContext), listener);

                Assert.Equal(@"<div class='a'>
    <i>x</i>
    <EM>y</EM>
    <h1>
        z
        <pre><code>
            a*b*c
        </code></pre>
    </h1>
</div>
<script>alert(1);</script>
".Replace("\r\n", "\n"), html);
            }
            Assert.Equal(3, listener.Items.Count);
            Assert.Equal(new[]
            {
                "Invalid tag(i)!",
                "Invalid tag(EM)!",
                "Invalid tag(script)!"
            }, from item in listener.Items select item.Message);
        }

        [Fact]
        [Trait("Related", "Validation")]
        public void TestHtmlInlineTagValidation()
        {
            var content = @"This is inline html: <div class='a'><i>x</i><EM>y</EM><h1>z<pre><code>a*b*c</code></pre></h1></div>

<script>alert(1);</script> end.";

            var builder = MarkdownValidatorBuilder.Create(
                null,
                new CompositionContainer(
                    new ContainerConfiguration()
                        .WithAssembly(typeof(ValidationTest).Assembly)
                        .CreateContainer()));

            builder.AddTagValidators(new[]
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
                    OpeningTagOnly = true
                },
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "pre" },
                    MessageFormatter = "Warning tag({0})!",
                    Behavior = TagValidationBehavior.Warning,
                },
            });
            
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(MarkdownValidatePhaseName);
            using (new LoggerPhaseScope(MarkdownValidatePhaseName))
            {
                var html = Markup(content, builder.CreateRewriter(DefaultContext), listener);

                Assert.Equal(@"<p>This is inline html: <div class='a'><i>x</i><EM>y</EM><h1>z<pre><code>a<em>b</em>c</code></pre></h1></div></p>
<script>alert(1);</script> end.
".Replace("\r\n", "\n"), html);
            }
            Assert.Equal(7, listener.Items.Count);
            Assert.Equal(new[]
            {
                "Invalid tag(div)!",
                "Invalid tag(EM)!",
                "Warning tag(h1)!",
                "Warning tag(pre)!",
                "Warning tag(pre)!",
                "Warning tag(h1)!",
                "Warning tag(script)!"
            }, from item in listener.Items select item.Message);
        }

        [Fact]
        [Trait("Related", "Validation")]
        public void TestTokenValidator()
        {
            const string content = "# Hello World";
            const string expected = "<h1>Hello World</h1>\n";
            const string expectedMessage = "a space is expected after '#'";
            string message = null;

            var rewriter = MarkdownObjectRewriterFactory.FromValidator(
                MarkdownObjectValidatorFactory.FromLambda<HeadingBlock>(
                    block =>
                    {
                        if (!block.Lines.ToString().StartsWith("# "))
                        {
                            message = expectedMessage;
                        }
                    })
                );

            var html = Markup(content, rewriter, null);
            Assert.Equal(expected.Replace("\r\n", "\n"), html);
            Assert.Equal(expectedMessage, message);
        }

        [Fact]
        [Trait("Related", "Validation")]
        public void TestValidatorWithContext()
        {
            const string content = @"# Title-1
# Title-2";
            const string expected = @"<h1>Title-1</h1>
<h1>Title-2</h1>
";
            const string expectedMessage = "expected one title in one document.";
            string message = null;

            var context = new Dictionary<string, object>();
            var validator = MarkdownObjectValidatorFactory.FromLambda<HeadingBlock>(
                block =>
                {
                    if (block.Level == 1)
                    {
                        if (context.TryGetValue("count", out object countObj) && countObj is int count)
                        {
                            context["count"] = count + 1;
                        }
                    }
                },
                tree =>
                {
                    context.Add("count", 0);
                },
                tree =>
                {
                    if (context.TryGetValue("count", out object countObj) && countObj is int count)
                    {
                        if (count != 1)
                        {
                            message = expectedMessage;
                        }
                    }
                }
                );

            var rewriter = MarkdownObjectRewriterFactory.FromValidator(validator);
            var html = Markup(content, rewriter, null);
            Assert.Equal(expected.Replace("\r\n", "\n"), html);
            Assert.Equal(expectedMessage, message);
        }

        [Fact]
        [Trait("Related", "Validation")]
        public void TestMarkdownDocumentValidator()
        {
            const string content = "## Hello World";
            const string expected = "<h2>Hello World</h2>\n";
            const string expectedMessage = "H1 should be in the first line";
            string message = null;

            var rewriter = MarkdownObjectRewriterFactory.FromValidator(
                MarkdownObjectValidatorFactory.FromLambda<MarkdownDocument>(
                    root =>
                    {
                        if (root.First() is HeadingBlock heading)
                        {
                            if (heading.Level != 1)
                            {
                                message = expectedMessage;
                            }
                        }
                    })
                );

            var html = Markup(content, rewriter, null);
            Assert.Equal(expected.Replace("\r\n", "\n"), html);
            Assert.Equal(expectedMessage, message);
        }

        [Fact]
        [Trait("Related", "Validation")]
        public void TestGetSchemaName()
        {
            const string expectedSchemaName = "YamlMime:ModuleUnit";
            const string yamlFilename = "moduleunit.yml";
            const string yamlContent = @"### YamlMime:ModuleUnit
uid: learn.azure.introduction";
            File.WriteAllText(yamlFilename, yamlContent);
            InclusionContext.PushFile(yamlFilename);
            InclusionContext.PushInclusion("introduction-included.md");

            string schemaName = string.Empty;

            var rewriter = MarkdownObjectRewriterFactory.FromValidator(
               MarkdownObjectValidatorFactory.FromLambda<MarkdownDocument>(
                   root =>
                   {
                       schemaName = root.GetData("SchemaName")?.ToString();
                   })
               );
            var html = Markup("# Hello World", rewriter, null);
            Assert.Equal(expectedSchemaName, schemaName);
        }

        private string Markup(string content, IMarkdownObjectRewriter rewriter, TestLoggerListener listener = null)
        {
            var pipelineBuilder = new MarkdownPipelineBuilder();
            var documentRewriter = new MarkdownDocumentVisitor(rewriter);
            pipelineBuilder.DocumentProcessed += document =>
            {
                ValidationExtension.SetSchemaName(document);
                documentRewriter.Visit(document);
            };
            var pipeline = pipelineBuilder.Build();

            if (listener != null)
            {
                Logger.RegisterListener(listener);
            }

            var html = Markdown.ToHtml(content, pipeline);
            if (listener != null)
            {
                Logger.UnregisterListener(listener);
            }

            return html;
        }
    }
}