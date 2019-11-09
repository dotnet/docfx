# Validate your Markdown files

In Markdown, it is possible to write any type of content, as long as the used syntax is valid. For example, Markdown supports the direct use of HTML tags - one can use the `<h1>title</h1>` syntax instead of conventional Markdown, such as `#title`.

With full-fledged HTML support, some behaviors might not be desirable. For example, you may not want to allow `<script>` tags included in Markdown, as that can introduce arbitrary JavaScript into documentation.

In this document, you'll learn how to define Markdown validation rules, which will help you ensure that your document follows strict conventions.

>[!NOTE]
>Markdown validation is part of the Markdown processor in DocFX. If you switch the Markdown engine, validation rules might not apply the same way.

There are three kinds of validation rules provided by DocFX:

1. [**HTML tag rules**](#html-tag-validation-rules). Used to validate HTML tags in Markdown content. There is often a need to restrict usage of HTML tags in Markdown to only allow safe markup.
2. [**Markdown token rules**](#markdown-token-validation-rules). This rule type can be used to validate different kinds of Markdown syntax elements, such as headings, links or images.
3. [**Metadata rules**](#validate-metadata-in-markdown-files). This rule type can be used to validate document metadata. Metadata can be defined in YAML front-matter in individual Markdown files, the `docfx.json` configuration file, or a standalone JSON file. Metadata rules give you a central place to validate metadata against specific document tagging conventions.

## HTML tag validation rules

In most cases, there is a need to limit the use of specific HTML tags in Markdown files. This is helpful in ensuring that the content is consistent and follows a documentation standard that is applicable to your project or organization.

To define a new HTML tag rule, create a `md.style` file with content similar to the snippet below:

```json
{
   "tagRules": [
      {
         "tagNames": [ "H1", "H2" ],
         "relation": "In",
         "behavior": "Warning",
         "messageFormatter": "Please do not use <H1> and <H2>, use '#' and '##' instead.",
         "customValidatorContractName": null,
         "openingTagOnly": false
      }
   ]
}
```

With this rule in place, anytime a `<H1>` or `<H2>` tag is used in a Markdown file, the DocFX build will produce a warning.

You can use the following properties to configure the HTML tag rule:

| Property | Description |
|:---------|:------------|
| `tagNames` | The list of HTML tag names to validate, *required*, *case-insensitive*. |
| `relation` | Optional for `tagNames`.<br/><br/>Possible values:<br/><ul><li>`In` - when HTML tag is in `tagNames`, this is default value.</li><li>`NotIn` - when HTML tag is not in `tagNames`.</li></ul> |
| `behavior` | (**Required**) Defines the behavior for when the HTML tag rule is triggered.<br/></br>Possible values:<br/><ul><li>`None` - Do nothing.</li><li>`Warning` - Log a warning.</li><li>`Error` - Log an error and stop the build.</li></ul> |
| `messageFormatter` | (**Required**) The log message displayed in the build output when the rule is triggered.<br/><br/>Can contain the following variables:<br/><ul><li>`{0}` - the name of tag.</li><li>`{1}` - the whole tag.<li></ul><br/><br/>For example, the `messageFormatter` can be set to `{0} is the tag name of {1}.`. When the `<H1 class="heading">` string will trigger the rule, the build output will contain: `H1 is the tag name of <H1 class="heading">.` |
| `customValidatorContractName` | An optional extension tag rule contract name for complex validation rules. See [Create a custom HTML tag rule](#create-a-custom-html-tag-rule) for details on creating custom rules. |
| `openingTagOnly` | Optional Boolean value that determines whether the document is scanned for opening tags only, or whether closing tags are required. Default is `false`. |

### Testing the rule

To enable and test the newly-created rule, place the `md.style` file in the same folder where `docfx.json` is located, then run `docfx`. If you followed the example above, a warning will be shown if `<H1>` or `<H2>` tags are encountered during build.

### Creating a custom HTML tag rule

By default HTML tag rules only validate whether a HTML tag exists in Markdown files. In certain scenarios it might be important to validate the contents of the tag in addition to its presence. For example, you may not want a tag to contain `onclick` attributes,  as that can result in injected JavaScript on the documentation page.

To perform tag content validation, it is possible to create a custom rule. To do so, follow the steps below.

1. Create a new .NET project in your code editor (e.g. Visual Studio).
2. Add a reference to the [`Microsoft.DocAsCode.Plugins`](https://www.nuget.org/packages/Microsoft.DocAsCode.Plugins/) and [`Microsoft.Composition`](https://www.nuget.org/packages/Microsoft.Composition/) NuGet packages.
3. Create a new class and implement the `@Microsoft.DocAsCode.Plugins.ICustomMarkdownTagValidator` interface.
4. Add the `ExportAttribute` decorator with your contract name.

For example, to require for HTML links (`<a>`) to not include the `onclick` attribute, the code can be written as such:

```csharp
[Export("should_not_contain_onclick", typeof(ICustomMarkdownTagValidator))]
public class MyMarkdownTagValidator : ICustomMarkdownTagValidator
{
    public bool Validate(string tag)
    {
        // use Contains for demo purpose, a complete implementation should parse the HTML tag.
        return tag.Contains("onclick");
    }
}
```

Subsequently, the `md.style` file can be updated with a reference to the rule:

```json
{
   "tagRules": [
      {
         "tagNames": [ "a" ],
         "behavior": "Warning",
         "messageFormatter": "Please do not use 'onclick' in HTML link.",
         "customValidatorContractName": "should_not_contain_onclick",
         "openingTagOnly": true
      }
   ]
}
```

### How to enable custom HTML tag rules

1. Same as default HTML tag rule, config the rule in `md.style`.
2. Create a folder (`rules` for example) in your DocFX project folder, put all your custom rule assemblies to a `plugins` folder under `rules` folder.
   Now your DocFX project should look like this:

   ```
   /
   |- docfx.json
   |- md.style
   \- rules
      \- plugins
         \- <your_rule>.dll
   ```
3. Update your `docfx.json` with following content:

   ```json
   {
     ...
     "dest": "_site",
     "template": [
      "default", "rules"
     ]
   }
   ```
4. Run `docfx` you'll see your rule being executed.

> [!Note]
> The folder `rules` is actually a template folder. In DocFX, template is a place for you to customize build, render, validation behavior.
> For more information about template, please refer to our [template](howto_build_your_own_type_of_documentation_with_custom_plug-in.md) and [plugin](howto_build_your_own_type_of_documentation_with_custom_plug-in.md) documentation.

## Markdown token validation rules

Besides HTML tags, you may also want to validate Markdown syntax like heading or links. For example, in Markdown, you may want to limit code snippet to only support a set of languages.

To create such rule, follow the following steps:

1.  Create a project in your code editor (e.g. Visual Studio).
2.  Add nuget package `Microsoft.DocAsCode.MarkdownLite` and `Microsoft.Composition`.
3.  Create a class and implements @Microsoft.DocAsCode.MarkdownLite.IMarkdownTokenValidatorProvider
    > @Microsoft.DocAsCode.MarkdownLite.MarkdownTokenValidatorFactory contains some helper methods to create a validator.
4.  Add ExportAttribute with rule name.

For example, the following rule require all code block to be `csharp`:
```csharp
[Export("code_snippet_should_be_csharp", typeof(IMarkdownTokenValidatorProvider))]
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

To enable this rule, update your `md.style` to the following:

```json
{
    "rules": [ "code_snippet_should_be_csharp" ]
}
```

Then follow the same steps in [How to enable custom HTML tag rules](#how-to-enable-custom-html-tag-rules), run `docfx` you'll see your rule executed.

### Logging in your rules

As you can see in the above example, you can throw @Microsoft.DocAsCode.Plugins.DocumentException to raise an error, this will stop the build immediately.

You can also use @Microsoft.DocAsCode.Common.Logger.LogWarning(System.String,System.String,System.String,System.String) and @Microsoft.DocAsCode.Common.Logger.LogError(System.String,System.String,System.String,System.String) to report a warning and an error respectively.

> [!Note]
> To use these methods, you need to install nuget package `Microsoft.DocAsCode.Common` first.

The different between `ReportError` and throw `DocumentException` is throwing exception will stop the build immediately but `ReportError` won't stop build but will eventually fail the build after rules are run.

### Advanced: validating tokens with file context

For some cases, we need to validate some tokens with file context.

For example, we want each topic has one title (i.e. h1 written by markdown syntax, e.g. `# <title>`).
But you cannot count them in @Microsoft.DocAsCode.MarkdownLite.IMarkdownTokenValidator, it is shared by all files, and it will never be hit when there is no heading.

For this purpose, we need to create validator like following:

```csharp
MarkdownTokenValidatorFactory.FromLambda<MarkdownHeadingBlockToken>(
    t =>
    {
        if (t.Depth == 1)
        {
            var re = MarkdownTokenValidatorContext.CurrentRewriteEngine;
            var h1Count = (int)re.GetVariable("h1Count");
            re.SetVariable("h1Count", h1Count + 1);
        }
    },
    re =>
    {
        re.SetVariable("h1Count", 0);
        re.SetPostProcess("checkH1Count", re1 =>
        {
            var h1Count = (int)re.GetVariable("h1Count");
            if (h1Count != 1)
            {
                 Logger.LogError($"Unexpected title count: {h1Count}.");
            }
        })
    });
```

The [FromLambda](xref:Microsoft.DocAsCode.MarkdownLite.MarkdownTokenValidatorFactory.FromLambda``1(System.Action{``0},System.Action{Microsoft.DocAsCode.MarkdownLite.IMarkdownRewriteEngine})) method takes two callbacks:
* The first will be invoked on @Microsoft.DocAsCode.MarkdownLite.MarkdownHeadingBlockToken matched in all files.
  And the static property @Microsoft.DocAsCode.MarkdownLite.MarkdownTokenValidatorContext.CurrentRewriteEngine will provide current context object.
* The second will be invoked on starting a new file.
  And you can initialize some variables for each file, and register some callbacks when the file completed.

## Advanced usage of `md.style`

### Default rules

If a rule has the contract name of `default`, it will be enabled by default. You don't need to enable it in `md.style`.

### Enable/disable rules in `md.style`

You can add use `disable` to specify whether disable a rule:
```json
{
   "rules": [ { "contractName": "<contract_name>", "disable": true } ]
}
```

This gives you an opportunity to disable the rules enabled by default.

## Validate metadata in markdown files

In markdown file, we can write some metadata in [conceptual](../spec/docfx_flavored_markdown.md#yaml-header) or [overwrite document](intro_overwrite_files.md).
And we allow to add some plug-ins to validate metadata written in markdown files.

### Scope of metadata validation

Metadata is coming from multiple sources, the following metadata will be validated during build: 
1.  YAML header in markdown.
2.  Global metadata and file metadata in `docfx.json`.
3.  Global metadata and file metadata defined in separate `.json` files.

> [!Tip]
> For more information about global metadata and global metadata, see [docfx.json format](docfx.exe_user_manual.md#3-docfxjson-format).

### Create validation plug-ins

1.  Create a project in your code editor (e.g. Visual Studio).
2.  Add nuget package `Microsoft.DocAsCode.Plugins` and `Microsoft.Composition`.
3.  Create a class and implement @Microsoft.DocAsCode.Plugins.IInputMetadataValidator

For example, the following validator prohibits any metadata with name `hello`:

```csharp
[Export(typeof(IInputMetadataValidator))]
public class MyInputMetadataValidator : IInputMetadataValidator
{
    public void Validate(string sourceFile, ImmutableDictionary<string, object> metadata)
    {
        if (metadata.ContainsKey("hello"))
        {
            throw new DocumentException($"Metadata 'hello' is not allowed, file: {sourceFile}");
        }
    }
}
```

Enable metadata rule is same as other rules, just copy the assemblies to the `plugins` of your template folder and run `docfx`.

### Create configurable metadata validation plug-ins

There are two steps to create a metadata validator:

1.  We need to modify export attribute for metadata validator plug-in:

    ```csharp
    [Export("hello_is_not_valid", typeof(IInputMetadataValidator))]
    ```

    > [!Note]
    > If the rule doesn't have a contract name, it will be always enabled,
    > i.e., there is no way to disable it unless delete the assembly file.

2.  Modify `md.style` with following content:

    ```json
    {
      "metadataRules": [
        { "contractName": "hello_is_not_valid", "disable": false }
      ]
    }
    ```

## Advanced: Share your rules

Some users have a lot of document projects, and want to share validations for all of them, and don't want to write `md.style` file repeatedly.

### Create template
For this propose, we can create a template with following structure:

```
/  (root folder for plug-in)
\- md.styles
   |- <category-1>.md.style
   \- <category-2>.md.style
\- plugins
   \- <your_rule>.dll 
```

In `md.styles` folder, there is a set of definition files, with file extension `.md.style`, each file is a category.

In one category, there is a set of rule definition.

For example, create a file with name `test.md.style`, then write following content:

```json
{
   "tagRules": {
      "heading": {
         "tagNames": [ "H1", "H2" ],
         "behavior": "Warning",
         "messageFormatter": "Please do not use <H1> and <H2>, use '#' and '##' instead.",
         "openingTagOnly": true
      }
   },
   "rules": {
      "code": "code_snippet_should_be_csharp"
   },
   "metadataRules": {
      "hello": { "contractName": "hello_is_not_valid", "disable": true }
   }
}
```

Then `test` is the category name (from file name) for three rules, and apply different `id` for each rule, they are `heading`, `code` and `hello`.

When you build document with this template, all rules will be active when `disable` property is `false`.

### Config rules

Some rules need to be enabled/disabled in some special document project.

For example, `hello` rule is not required for most project, but for a special project, we want to enable it.

We need to modify `md.style` file in this document project with following content:

```json
{
   "settings": [
      { "category": "test", "id": "hello", "disable": false }
   ]
}
```

And for some project we need to disable all rules in test category:

```json
{
   "settings": [
      { "category": "test", "disable": true }
   ]
}
```

> [!Note]
> `disable` property is applied by following order:
> 1.  `tagRules`, `rules` and `metadataRules` in `md.style`.
> 2.  auto enabled `rules` with contract name `default`.
> 3.  `settings` with `category` and `id` in `md.style`.
> 4.  `settings` with `category` in `md.style`.
> 5.  `disable` property in definition file.
