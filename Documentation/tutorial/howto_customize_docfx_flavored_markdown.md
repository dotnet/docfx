How-to: Customize DocFX Flavored Markdown
=========================================

Customize Renderer
------------------

DocFX Flavored Markdown introduced @Microsoft.DocAsCode.Dfm.IDfmCustomizedRendererPartProvider from v2.17.
In older version, you need to define a new markdown renderer and export a new [markdown service](xref:Microsoft.DocAsCode.Build.EngineIMarkdownServiceProvider).

Now, you can customize a part of html renderer as a DocFX plugin.

### Default rendering for block code

For standard markdown, block code is following:
````md
```cs
Console.WriteLine();
```
````
And the html will be:
```html
<pre><code class="lang-cs">Console.WriteLine();
</code></pre>
```

### Set goal
Now we want any csharp code (`cs`, `c#`, `csharp`) will generate following html:
```html
<pre><code class="lang-csharp">Console.WriteLine();
</code></pre>
```

### Create customize rendering plugin project
To complete this goal, we need create a customize rendering plugin.

1. Create a project, set project names.
2. Add required nuget package: `Microsoft.DocAsCode.Dfm` with version >= 2.17.
3. Create a class, for example with name `CustomBlockCodeRendererPart`
4. Inhert @"Microsoft.DocAsCode.Dfm.DfmCustomizedRendererPartBase`3" (which implements @Microsoft.DocAsCode.Dfm.IDfmCustomizedRendererPart)
5. Implements renderer part class like following:

   ```cs
   public class CustomBlockCodeRendererPart : DfmCustomizedRendererPartBase<IMarkdownRenderer, MarkdownCodeBlockToken, MarkdownBlockContext>
   {
       public override string Name => "MyFirstCustomRendererPart";
   
       public override bool Match(IMarkdownRenderer renderer, MarkdownCodeBlockToken token, MarkdownBlockContext context)
       {
           return token.Lang == "cs" || token.Lang == "c#";
       }
   
       public override StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeBlockToken token, MarkdownBlockContext context)
       {
           StringBuffer result = "<pre><code class=\"";
           result += renderer.Options.LangPrefix;
           result += "csharp";
           result += "\">";
           result += token.Code;
           result += "\n</code></pre>";
           return result;
       }
   }
   ```
6. Create another class, for example with name `CustomBlockCodeRendererPartProvider`
7. Implements @Microsoft.DocAsCode.Dfm.IDfmCustomizedRendererPartProvider and export like following:

   ```cs
   [Export(typeof(IDfmCustomizedRendererPartProvider))]
   public class CustomBlockCodeRendererPartProvider : IDfmCustomizedRendererPartProvider
   {
       public IEnumerable<IDfmCustomizedRendererPart> CreateParts(IReadOnlyDictionary<string, object> parameters)
       {
           yield return new CustomBlockCodeRendererPart();
       }
   }
   ```
8. Build the project.

### Enable customize rendering plugin

1. Copy output assemblies to x\plugins, x is any folder.
2. Run `docfx.exe` with template x ([details](howto_build_your_own_type_of_documentation_with_custom_plug-in.html#enable-plug-in))

Customize Markdown Extension
----------------------------

In markdown lite, we can [customize markdown extension](intro_markdown_lite.html#how-to-customize-markdown-syntax) by following steps:
1. Create a new token
2. Create a new rule
3. Create a new renderer
4. Create a new engine builder

In DocFX Flavored Markdown, we introduced @Microsoft.DocAsCode.Dfm.IDfmEngineCustomizer from v2.17.

Now, we need to following step:
1. Create a new token
2. Create a new rule
3. Create a new renderer part
4. Create a new renderer part provider
5. Create a new DFM engine customizer

The steps do not decrease, but you can combine any number of customize markdown extension plugin as you like.

### How to create a new markdown extension by plugin
1. Define markdown syntax (same with [markdown lite](intro_markdown_lite.html#define-markdown-syntax)).
2. Select token kind (same with [markdown lite](intro_markdown_lite.html#select-token-kind)).
3. Define token (same with [markdown lite](intro_markdown_lite.html#define-token)).
4. Define rule (same with [markdown lite](intro_markdown_lite.html#define-rule)).
5. Create a new renderer part.

   ```cs
   public sealed class LabelRendererPart : DfmCustomizedRendererPartBase<IMarkdownRenderer, MarkdownMyLabelBlockToken, MarkdownBlockContext>
   {
       public override string Name => "LabelRendererPart";
   
       public override bool Match(IMarkdownRenderer renderer, MarkdownMyLabelBlockToken token, MarkdownBlockContext context) => true;
   
       public override StringBuffer Render(IMarkdownRenderer renderer, MarkdownMyLabelBlockToken token, MarkdownBlockContext context) => "<div id=\"" + token.Label + "\"></div>";
   }
   ```
6. Create a new renderer part provider.

   ```cs
   [Export(typeof(IDfmCustomizedRendererPartProvider))]
   public class LabelRendererPartProvider : IDfmCustomizedRendererPartProvider
   {
       public IEnumerable<IDfmCustomizedRendererPart> CreateParts(IReadOnlyDictionary<string, object> parameters)
       {
           yield return new LabelRendererPart();
       }
   }
   ```
7. Create a new DFM engine customizer.
   ```cs
   [Export(typeof(IDfmEngineCustomizer))]
   public class MyDfmEngineCustomizer : IDfmEngineCustomizer
   {
       public void Customize(DfmEngineBuilder builder, IReadOnlyDictionary<string, object> parameters)
       {
           var index = builder.BlockRules.FindIndex(r => r is MarkdownHeadingBlockRule);
           builder.BlockRules = builder.BlockRules.Insert(index, new MyHeadingRule());
       }
   }
   ```
8. Build project.
9. Enable and test your plugin.
