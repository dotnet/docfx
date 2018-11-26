# Markdown Lite

## Introduction

Markdown lite is a simple markdown tool to markup `md` file.

## Design goal

We write this tool for good extensibility, so our implementation should obey following principles:

1.  Extensibility:
    * Support markdown syntax extension.
    * Support validation extension.
2.  Correctness:
    We follow GFM syntax, but when some rules is too hard to implement, just breaking.
3.  Performance:
    Performance is not our major concern.

## Steps

There are three steps when calling [markup method](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownEngine.Markup(System.String,System.String)):
* [Parse](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownParser)
* [Rewrite](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownTokenRewriter) or [validate](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownTokenValidator)
* [Render](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownRenderer)

### Step 1: Parse

In this step, it will parse markdown text to [tokens](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownToken).
The parser is based on [rules](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownRule), which make up the [context](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownContext).

For example,
[heading token](xref:Microsoft.DocAsCode.MarkdownLite.MarkdownHeadingBlockToken) is created by [heading rule](xref:Microsoft.DocAsCode.MarkdownLite.MarkdownHeadingBlockRule),
the heading rule is belonging to [block context](xref:Microsoft.DocAsCode.MarkdownLite.MarkdownBlockContext).

### Step 2: Rewrite or validate

In this step, it will walk through all [tokens](xref:Microsoft.DocAsCode.MarkdownLite.IMarkdownToken),
we can change it to another, or just validate.

For example, we can create a rewriter to change all heading token with depth + 1:

```csharp
MarkdownTokenRewriterFactory.FromLambda<IMarkdownRewriteEngine, MarkdownHeadingBlockToken>(
    (engine, token) => new MarkdownHeadingBlockToken(token.Rule, token.Context, token.Content, token.Id, token.Depth + 1, token.SourceInfo);
```

### Step 3: Render

In this step, it renders models to text content (html format by default).
To simplify extension, we created an [adapter](xref:Microsoft.DocAsCode.MarkdownLite.MarkdownRendererAdapter),
the adapter invoke methods by following rules:

1.  Method name is `Render`
2.  Instance method
3.  Return type is @Microsoft.DocAsCode.MarkdownLite.StringBuffer
4.  The count of parameters is 3, and types are following:
    1.  @Microsoft.DocAsCode.MarkdownLite.IMarkdownRenderer or any type implements it.
    2.  @Microsoft.DocAsCode.MarkdownLite.IMarkdownToken or any type implements it.
    3.  @Microsoft.DocAsCode.MarkdownLite.IMarkdownContext or any type implements it.
5.  Always invoke the best overloaded method (The best is defined by [binder](https://msdn.microsoft.com/en-us/library/microsoft.csharp.runtimebinder.binder.invoke(v=vs.110).aspx)).

## Engine and engine builder

Engine is a set of parser, rewriter and renderer.
It can markup a markdown file to html file (or others).
But it cannot be invoked in parallel.

So we create an [engine builder](xref:Microsoft.DocAsCode.MarkdownLite.MarkdownEngineBuilder).
It defines all the rules of parser, rewriter and renderer.
It can create instances when needed.

## How to customize markdown syntax

### Define markdown syntax

Define markdown:

```md
: My label
```

should be rendered as following html:

```html
<div id="My label"></div>
```

### Select token kind

First of all, we should select the context for this rule.
And in this goal, the new line is required.
So it should be a [block token](https://daringfireball.net/projects/markdown/syntax#block), all of the names for class should contain `Block`.

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

Create a rule class as following:

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
        return new MarkdownMyLabelBlockToken(this, parser.Context, match.Groups[1].Value, sourceInfo);
    }
}
```

### Define renderer

Create a renderer class as following:

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

Create an engine builder class as following:

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
