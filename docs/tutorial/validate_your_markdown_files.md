# Validate your Markdown files

In Markdown, it is possible to write any type of content, as long as the used syntax is valid. For example, Markdown supports the direct use of HTML tags - one can use the `<h1>title</h1>` syntax instead of conventional Markdown, such as `#title`.

With full-fledged HTML support, some behaviors might not be desirable. For example, you may not want to allow `<script>` tags included in Markdown, as that can introduce arbitrary JavaScript into documentation.

In this document, you'll learn how to define Markdown validation rules, which will help you ensure that your document follows strict conventions.

>[!NOTE]
>Markdown validation is part of the `dfm` Markdown processor in DocFX. If you switch the Markdown engine, validation rules might not apply the same way.

There are three kinds of validation rules provided by DocFX:

1. [**HTML tag rules**](#html-tag-validation-rules). Used to validate HTML tags in Markdown content. There is often a need to restrict usage of HTML tags in Markdown to only allow safe markup.
2. [**Markdown token rules**](#markdown-token-validation-rules). This rule type can be used to validate different kinds of Markdown syntax elements, such as headings, links or images.
3. [**Metadata rules**](#validate-metadata-in-markdown-files). This rule type can be used to validate document metadata. Metadata can be defined in the YAML header in individual Markdown files, the `docfx.json` configuration file, or a standalone JSON file. Metadata rules give you a central place to validate metadata against specific document tagging conventions.

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
3. Create a new class and implement the @Microsoft.DocAsCode.Plugins.ICustomMarkdownTagValidator interface.
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

Build the project, to make sure that you have an assembly that contains the compiled contract. Subsequently, the `md.style` file can be updated with a reference to the contract, as specified in code:

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

### Integrating the custom rule into the build

1. Just as it's done for built-in HTML tag rules, configure the rule in the `md.style` file.
2. Create a new folder in your DocFX project directory (`rules`, for example) and place all your custom rule assemblies to a `plugins` folder under the `rules` directory. Your DocFX project should look like this:

   ```text
   /
   |- docfx.json
   |- md.style
   \- rules
      \- plugins
         \- <your_rule>.dll
   ```

3. Update your `docfx.json` to include a reference to the `rules` folder:

   ```json
   {
     ...
     "dest": "_site",
     "template": [
        "default", "rules"
     ]
   }
   ```

4. Run `docfx` in your project folder. New rules will be executed and the build output will capture any triggers.

>[!NOTE]
>The `rules` folder is a template folder. In DocFX, templates are a place to customize the build, rendering and validation behaviors.
>For more information about templates, please refer to our [template documentation](howto_build_your_own_type_of_documentation_with_custom_plug-in.md) and [plugin documentation](howto_build_your_own_type_of_documentation_with_custom_plug-in.md).

## Markdown token validation rules

Besides HTML tags, you may also want to validate Markdown syntax like headings or links. This is helpful if you want to implement scenarios such as limiting code snippets to only support a set of pre-defined programming language identifiers.

To create a rule, follow the steps below:

1. Create a new project in your IDE (e.g. Visual Studio).
2. Add a reference to the [`Microsoft.DocAsCode.MarkdownLite`](https://www.nuget.org/packages/Microsoft.DocAsCode.MarkdownLite/) and [`Microsoft.Composition`](https://www.nuget.org/packages/Microsoft.Composition/) NuGet packages.
3. Create a class that implements the @Microsoft.DocAsCode.MarkdownLite.IMarkdownTokenValidatorProvider interface.
    > @Microsoft.DocAsCode.MarkdownLite.MarkdownTokenValidatorFactory contains some helper methods to create a validator.
4. Decorate your class with the `ExportAttribute`, that contains the rule name.

For example, the following rule will require all code blocks to use the `csharp` language identifier:

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

To enable this rule, update your `md.style` with the following rule flag:

```json
{
    "rules": [ "code_snippet_should_be_csharp" ]
}
```

Follow the steps in [How to enable custom HTML tag rules](#how-to-enable-custom-html-tag-rules) to configure the plugin and run `docfx` in the project folder. You'll see your rule picked up by the build.

### Logging in your rules

You can throw @Microsoft.DocAsCode.Plugins.DocumentException to raise an error with the rules. This will stop the build immediately.

You can also use @Microsoft.DocAsCode.Common.Logger.LogWarning(System.String,System.String,System.String,System.String) and @Microsoft.DocAsCode.Common.Logger.LogError(System.String,System.String,System.String,System.String) to report a warning or an error, respectively.

>[!NOTE]
>To use the aforementioned methods, you will need to install the [`Microsoft.DocAsCode.Common`](https://www.nuget.org/packages/Microsoft.DocAsCode.Common/) NuGet package.

The difference between `LogError` and throwing `DocumentException` is in the fact that throwing the exception will stop the build immediately. `LogError` won't stop the build but will report a failure once the rest of the execution is complete.

### Advanced: validating tokens with file context

In certain cases, we might need to validate tokens with the file context. For example, it might be necessary to enforce a rule that ensures that each topic has one title (i.e. H1 written in standard Markdown syntax, e.g. `# <title>`).

You can't directly count the tokens with @Microsoft.DocAsCode.MarkdownLite.IMarkdownTokenValidator since the context is shared by all files - the rule will never be hit when there is no heading in a file.

We can create a custom validator as such:

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

* The first callback will be invoked in @Microsoft.DocAsCode.MarkdownLite.MarkdownHeadingBlockToken, matched against all files. The static @Microsoft.DocAsCode.MarkdownLite.MarkdownTokenValidatorContext.CurrentRewriteEngine property will provide current context object.
* The second callback will be invoked when starting the processing of a new file. You can initialize some variables for each file, and register some callbacks when the file processing is complete.

## Advanced usage of `md.style`

### Default rules

If a rule has the `default` contract name, it will be enabled by default. You don't need to enable it in `md.style`.

### Enable/disable rules in `md.style`

You can use the `disable` property to specify whether a rule needs to be disabled:

```json
{
   "rules": [ { "contractName": "<contract_name>", "disable": true } ]
}
```

This gives you an opportunity to disable the rules enabled by default.

## Validate metadata in Markdown files

In Markdown files, we can write metadata in [the YAML header](../spec/docfx_flavored_markdown.md#yaml-header) or [an overwrite document](intro_overwrite_files.md).
DocFX allows you to create a plug-in to validate metadata.

### Scope of metadata validation

Metadata will be validated by the DocFX build in the following order:

1. YAML header in the Markdown file.
2. Global metadata and file metadata in `docfx.json`.
3. Global metadata and file metadata defined in separate `.json` files.

>[!TIP]
>For more information about global metadata, check out the [documentation on `docfx.json`](docfx.exe_user_manual.md#3-docfxjson-format).

### Create validation plug-ins

1. Create a new project in your IDE (e.g. Visual Studio).
2. Add a reference to [`Microsoft.DocAsCode.Plugins`](https://www.nuget.org/packages/Microsoft.DocAsCode.Plugins/) and [`Microsoft.Composition`](https://www.nuget.org/packages/Microsoft.Composition/) NuGet packages.
3. Create a new class and implement the @Microsoft.DocAsCode.Plugins.IInputMetadataValidator.

For example, the following validator prohibits any metadata with the name set to `hello`:

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

Enable the metadata rule the same way as outlined above - copy the compiled assemblies to the `plugins` directory in your project and run `docfx`.

### Create configurable metadata validation plug-ins

There are two steps to create a metadata validator:

1. Modify the `ExportAttribute` for the metadata validator plug-in class to specify its type:

    ```csharp
    [Export("hello_is_not_valid", typeof(IInputMetadataValidator))]
    ```

    >[!NOTE]
    >If the rule doesn't have a contract name, it will be always enabled, i.e. there is no way to disable it unless the assembly files are deleted.

2. Modify the `md.style` file with the following content:

    ```json
    {
      "metadataRules": [
        { "contractName": "hello_is_not_valid", "disable": false }
      ]
    }
    ```

## Advanced: Sharing your rules

Some users might have a number of documentation projects, and may want to share validation rules between them. In such a scenario, writing `md.style` files repeatedly is sub-optimal.

### Create a template

For this propose, we can create a template with following structure:

```text
/  (root folder for plug-in)
\- md.styles
   |- <category-1>.md.style
   \- <category-2>.md.style
\- plugins
   \- <your_rule>.dll 
```

The `md.styles` folder will contain a set of definition files, with the file extension set to `.md.style` (_each file is a category_).

Each category file contains a set of rule definitions.

For example, you can create a `test.md.style` file, then include the following rules:

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

`test` is the category name (_taken from file name_) for three rules. A different identifier is applied for each rule - `heading`, `code` and `hello`.

When you build your documentation with this template, all aforementioned rules will be active when the `disable` property is set to `false`.

### Config rules

Some rules need to be enabled or disabled in certain documentation projects. For example, the `hello` rule might not be required for most of your projects, but for others it might be necessary.

To configure this scenario, you will need to modify the `md.style` file in your document project with the following settings:

```json
{
   "settings": [
      { "category": "test", "id": "hello", "disable": false }
   ]
}
```

And for other projects, you will need to disable all rules in test category:

```json
{
   "settings": [
      { "category": "test", "disable": true }
   ]
}
```

>[!NOTE]
>The `disable` property is applied in the following order:
>
>1. `tagRules`, `rules` and `metadataRules` in `md.style`.
>2. Automatically enabled `rules` with contract names set to `default`.
>3. `settings` with `category` and `id` in `md.style`.
>4. `settings` with `category` in `md.style`.
>5. `disable` property in definition file.
