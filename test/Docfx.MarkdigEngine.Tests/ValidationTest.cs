// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Docfx.Common;
using Docfx.MarkdigEngine.Extensions;
using Docfx.Plugins;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

public class ValidationTest
{
    public const string MarkdownValidatePhaseName = "Markdown style";

    private readonly MarkdownContext DefaultContext =
        new(
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

        var builder = MarkdownValidatorBuilder.Create(null);

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

        var builder = MarkdownValidatorBuilder.Create(null);
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

        var builder = MarkdownValidatorBuilder.Create(null);

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
