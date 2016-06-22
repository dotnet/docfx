# Validate your markdown files

In GFM, we can write any document with right syntax. For example, we can write html tag `<h1>title</h1>` instead of markdown syntax `#title`. But for some purpose, some behaviors are unwanted, so we add markdown validators for each markdown document.

In this document, you'll learn how to define markdown validation rules, which will help you to validate markdown documents in an efficient way.

*This is part of DFM, if you switch markdown engine to other engine, validation might not work.*

## How to config markdown validation rules

Create a *JSON* file with name `md.style` in the same folder of `docfx.json`, and with following content:
```json
{
   "rules": [],
   "tagRules": []
}
```

## Build-in html tag validation rules

For most cases, we might need some html tag rules, and we have a build-in tag rules.

For example, create `md.style` with following content:
```json
{
   "tagRules": [
      {
         "tagName": [ "H1", "H2" ],
         "behavior": "Warning",
         "messageFormatter": "Some message.",
         "customValidatorContractName": null,
         "openingTagOnly": true
      }
   ]
}
```
Then when any one write `<H1>` or `<H2>` in markdown file, it will give a warning with message `Some message.`.

1.  `tagName` is filter html tag names, and they are case-insensitive.
2.  `behavior` is an enum value, it can be following:
    * None
    * Warning
    * Error
3.  `messageFormatter` can contain following variables:
    * `{0}` the name of tag.
    * `{1}` the whole tag.
    For example, the `messageFormatter` is `{0} is the tag name of {1}.`, and the tag is `<H1 class="heading">` match the rule, then it will output following message: `H1 is the tag name of <H1 class="heading">.`
4.  `customValidatorContractName` is an extension tag rule contract name for complex validation rule, see [How to create a custom html tag validator](#how-to-create-a-custom-html-tag-validator).
5.  `openingTagOnly` is a boolean, if `true`, it will only apply to opening tag, e.g. `<H1>`, otherwise, it will also apply to closing tag, e.g. `</H1>`.

### How to create a custom html tag validator
1.  Create a project in your code editor (e.g. visual studio).
2.  Add nuget package `Microsoft.DocAsCode.Plugins` and `Microsoft.Composition`.
3.  Create a class and implements @Microsoft.DocAsCode.Plugins.ICustomMarkdownTagValidator.
4.  Add ExportAttribute with contract name.

For example, we require tags should contain text `class`:
```csharp
[Export("Require text class", typeof(ICustomMarkdownTagValidator))]
public class MyMarkdownTagValidator : ICustomMarkdownTagValidator
{
    public bool Validate(string tag)
    {
        return tag.Contains("class");
    }
}
```

## Customize token validation rules
In `rules` property, we can add any [validation plug-in](#how-to-create-a-plug-in-for-markdown-validation) by following style:
```json
{
   "rules": [ { "name": "rule name", "disable": false } ]
}
```
or simplify it to:
```json
{
    "rules": [ "rule name" ]
}
```

### With contract name `default`
Any rules with contract name `default` will be enabled by default. This means when no `md.style` file, it is same as a `md.style` file with following content:
```json
{
   "rules": [ { "name": "default", "disable": false } ]
}
```
And you can disable default rules by following config:
```json
{
   "rules": [ { "name": "default", "disable": true } ]
}
```

### How to create a plug-in for markdown validation
1.  Create a project in your code editor (e.g. visual studio).
2.  Add nuget package `Microsoft.DocAsCode.MarkdownLite` and `Microsoft.Composition`.
3.  Create a class and implements @Microsoft.DocAsCode.MarkdownLite.IMarkdownTokenValidatorProvider

    > [!TIP]
    > @Microsoft.DocAsCode.MarkdownLite.MarkdownTokenValidatorFactory contains some help methods to create a validator.
4.  Add ExportAttribute with contract name, and if contract name is `default` it will apply automatically.


For example, we require all code block with language `csharp`:
```csharp
[Export("default", typeof(IMarkdownTokenValidatorProvider))]
public class MyMarkdownTokenValidatorProvider : IMarkdownTokenValidatorProvider
{
    public ImmutableArray<IMarkdownTokenValidator> GetValidators()
    {
        return ImmutableArray.Create(
            MarkdownTokenValidatorFactory.FromLambda<MarkdownCodeBlockToken>(t =>
            {
                if (t.Lang != "csharp")
                {
                     throw new DocumentException($"Code lang {t.Lang} is not valid, in file: {t.SourceInfo.File}, at line: {t.SourceInfo.LineNumber}");
                }
            }));
    }
}
```

## How to enable custom validators
1.  Config markdown validation rules in `md.style`.
2.  Copy your extension assemblies to `plugin` folder in your `DocFX.exe` or template.
