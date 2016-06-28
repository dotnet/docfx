# Markdown Lite

## Introduction

Markdown lite is a simple markdown tool to markup `md` file.

## Design goal

We write this tool for good extensiblity, so our implements should obay following order:

1.  Extensiblity:
    * Support markdown syntax extension.
    * Support validation extension.
2.  Correctness:
    We follow GFM syntax, but when some rule is too hard to implement, just breaking.
3.  Performance:
    We almost don't care performance.

## Steps

There are three step when calling [markup method](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownEngine.Markup(System.String,System.String)):
* [Parse](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownParser)
* [Rewrite](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownTokenRewriter) or [validate](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownTokenValidator)
* [Render](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownRenderer)

### Parse

In this step, it will parse markdown text to [models](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownToken).
And in this phase, it is [rule](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownRule) based.
Then a set of rule is called [context](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownContext).

For example,
[heading token](xref:Microsoft.DocAsCode.MarkdownLite.MarkdownHeadingBlockToken) is created by [heading rule](xref:Microsoft.DocAsCode.MarkdownLite.MarkdownHeadingBlockRule),
the heading rule is belong to [block context](xref:Microsoft.DocAsCode.MarkdownLite.MarkdownBlockContext).

### Rewrite or validate

In this step, it will walk through all [models](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownToken),
we can changed it to another, or just validate the content.

For example, we can create a rewriter to change all heading token with depth + 1:

```csharp
MarkdownTokenRewriterFactory.FromLambda<IMarkdownRewriteEngine, MarkdownHeadingBlockToken>(
    (engine, token) => new MarkdownHeadingBlockToken(token.Rule, token.Context, token.Content, token.Id, token.Depth + 1, token.SourceInfo);
```

### Render

In this step, it will render to text content (html format for normal).
To simplify extension, we create an [adapter](xref:Microsoft.DocAsCode.MarkdownLite.MarkdownRendererAdapter),
any class with following method will treat as render method:

1.  Method name is `Render`
2.  Instance method
3.  Return type is @Microsoft.DocAsCode.MarkdownLite.StringBuffer
4.  The count of parameters is 3, and type is following:
    1.  @Microsoft.DocAsCode.MarkdownLite.IMarkdownRenderer or any type implements it.
    2.  @Microsoft.DocAsCode.MarkdownLite.IMarkdownToken or any type implements it.
    3.  @Microsoft.DocAsCode.MarkdownLite.IMarkdownContext or any type implements it.
5.  Adapter alway invoke the most match method.

## Engine and engine builder

Engine is a set of parse, rewrite and render.
It can markup a markdown file to html file (or others).
But it cannot be invoked parallel.

So we create an [engine builder](xref:Microsoft.DocAsCode.MarkdownLite.MarkdownEngineBuilder).
It define all the rules of parser, rewriter and renderer.
It can create instances when needed.

## How to custom markdown syntax

### Set goal

Define markdown:

```
: My label
```

should be render as following html:

```html
<div id="My label"></div>
```

### Select context

First of all, we should select the context for this rule.
And in this goal, it should be block context.

### Define token

Create a token class like following:

```csharp
public class MarkdownMyLabelBlockToken : IMarkdownToken
{
    public MarkdownMyLabelBlockToken(IMarkdownRule rule, IMarkdownContext context, string label, SourceInfo sourceInfo)
    {
        Rule = rule;
        Context = context;
        Label = label;
        SourceInfo = sourceInfo;
    }

    public IMarkdownRule Rule { get; }

    public IMarkdownContext Context { get; }

    public string Label { get; }

    public SourceInfo SourceInfo { get; }
}

```

### Define rule

Create a rule class like following:

```csharp
public class MarkdownMyLabelBlockRule : IMarkdownRule
{
    public virtual string Name => "My Label";

    public virtual Regex LabelRegex { get; } = new Regex("^\: *([^\n]+?) *(?:\n+|$)");

    public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
    {
        var match = LabelRegex.Match(context.CurrentMarkdown);
        if (match.Length == 0)
        {
            return null;
        }
        var sourceInfo = context.Consume(match.Length);
        return new MarkdownMyLabelBlockToken(this, parser.Context, match.Group[1].Value, sourceInfo);
    }
}
```

### Define renderer

Create a renderer class like following:

```csharp
public class MyRenderer : HtmlRenderer
{
    public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownMyLabelBlockToken token, IMarkdownContext context)
    {
        return StringBuffer.Empty + "<div id=\"" + token.Label + "\"></div>";
    }
}
```

### Define engine builder

Create a engine builder class like following:

```csharp
public class MyEngineBuilder : GfmEngineBuilder
{
    public MyEngineBuilder(Options options) : base(options)
    {
         BlockRules = BlockRules.Insert(0, new MarkdownMyLabelBlockRule());
    }
}
```

### Markup it!

Then use your custom markdown in your code:

```csharp
public string MarkupMyMarkdown(string markdown)
{
    var builder = new MyEngineBuilder(new Options());
    var engine = builder.CreateEngine(new MyRender())
    return engine.Markup(markdown);
}
```