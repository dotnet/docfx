// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using HtmlReaderWriter;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class TemplateEngine
{
    private readonly ErrorBuilder _errors;
    private readonly Config _config;
    private readonly Package _package;
    private readonly Lazy<TemplateDefinition> _templateDefinition;
    private readonly JObject _global;
    private readonly LiquidTemplate _liquid;
    private readonly ThreadLocal<JavaScriptEngine> _js;
    private readonly MustacheTemplate _mustacheTemplate;
    private readonly string _locale;
    private readonly CultureInfo _cultureInfo;
    private readonly SearchIndexBuilder? _searchIndexBuilder;
    private readonly BookmarkValidator? _bookmarkValidator;

    public static TemplateEngine CreateTemplateEngine(
        ErrorBuilder errors,
        Config config,
        PackageResolver packageResolver,
        string locale,
        BookmarkValidator? bookmarkValidator = null,
        SearchIndexBuilder? searchIndexBuilder = null)
    {
        var template = config.Template;
        var templateFetchOptions = PackageFetchOptions.DepthOne;
        if (template.Type == PackageType.None)
        {
            template = new("_themes");
            templateFetchOptions |= PackageFetchOptions.IgnoreDirectoryNonExistedError;
        }
        var package = packageResolver.ResolveAsPackage(template, templateFetchOptions);

        return new TemplateEngine(errors, config, package, locale, bookmarkValidator, searchIndexBuilder);
    }

    public static TemplateEngine CreateTemplateEngine(ErrorBuilder errors, Config config, string locale, Package package)
    {
        return new TemplateEngine(errors, config, package, locale);
    }

    private TemplateEngine(
         ErrorBuilder errors,
         Config config,
         Package package,
         string locale,
         BookmarkValidator? bookmarkValidator = null,
         SearchIndexBuilder? searchIndexBuilder = null)
    {
        _errors = errors;
        _config = config;
        _locale = locale;
        _cultureInfo = LocalizationUtility.CreateCultureInfo(_locale);
        _package = package;
        _templateDefinition = new(() => _package.TryLoadYamlOrJson<TemplateDefinition>(errors, "template") ?? new());
        _global = LoadGlobalTokens(errors);
        _liquid = new(_package, _config.TemplateBasePath, _global);
        _js = new(() => JavaScriptEngine.Create(_package, _global));
        _mustacheTemplate = new(_package, "ContentTemplate", _global);
        _bookmarkValidator = bookmarkValidator;
        _searchIndexBuilder = searchIndexBuilder;
    }

    public string RunLiquid(ErrorBuilder errors, SourceInfo<string?> mime, TemplateModel model)
    {
        var layout = model.RawMetadata?.Value<string>("layout") ?? mime.Value ?? throw new InvalidOperationException();

        var liquidModel = new JObject
        {
            ["content"] = model.Content,
            ["page"] = model.RawMetadata,
            ["metadata"] = model.PageMetadata,
        };

        return _liquid.Render(errors, layout, mime, liquidModel);
    }

    public string RunMustache(ErrorBuilder errors, string templateName, JToken pageModel)
    {
        return _mustacheTemplate.Render(errors, templateName, pageModel);
    }

    public JToken RunJavaScript(string scriptName, JObject model, string methodName = "transform")
    {
        var scriptPath = new PathString($"ContentTemplate/{scriptName}");
        if (!_package.Exists(scriptPath))
        {
            return model;
        }

        var result = _js.Value!.Run(scriptPath, methodName, model);
        if (result is JObject obj && obj.TryGetValue("content", out var token) &&
            token is JValue value && value.Value is string content)
        {
            try
            {
                return JsonUtility.Parse(new ErrorList(), content, new FilePath("file"));
            }
            catch
            {
                return result;
            }
        }
        return result;
    }

    public void CopyAssetsToOutput(Output output, bool selfContained = true)
    {
        if (!selfContained || _templateDefinition.Value.Assets.Length <= 0)
        {
            return;
        }

        var glob = GlobUtility.CreateGlobMatcher(_templateDefinition.Value.Assets);

        Parallel.ForEach(_package.GetFiles(), file =>
        {
            if (glob(file))
            {
                output.Copy(file, _package, file);
            }
        });
    }

    public string? GetToken(string key)
    {
        return _global[key]?.ToString();
    }

    public (TemplateModel model, JObject metadata) CreateTemplateModel(FilePath file, string? mime, JObject pageModel)
    {
        var content = CreateContent(file, mime, ref pageModel);

        if (_config.DryRun)
        {
            return (new TemplateModel("", new JObject(), "", ""), new JObject());
        }

        // Hosting layers treats empty content as 404, so generate an empty <div></div>
        if (string.IsNullOrWhiteSpace(content))
        {
            content = "<div></div>";
        }

        var jsName = $"{mime}.mta.json.js";
        var temp = RunJavaScript(jsName, pageModel);
        var templateMetadata = temp as JObject ?? new JObject();

        if (JsonSchemaProvider.IsLandingData(mime))
        {
            templateMetadata.Remove("conceptual");
        }

        // content for *.mta.json
        var metadata = new JObject(templateMetadata.Properties().Where(p => !p.Name.StartsWith("_")))
        {
            ["is_dynamic_rendering"] = true,
        };

        var pageMetadata = HtmlUtility.CreateHtmlMetaTags(metadata);

        // content for *.raw.page.json
        var model = new TemplateModel(content, templateMetadata, pageMetadata, "_themes/");

        return (model, metadata);
    }

    private string ProcessHtml(ErrorBuilder errors, FilePath file, string html)
    {
        var bookmarks = new HashSet<string>();
        var searchText = new StringBuilder();

        var result = HtmlUtility.TransformHtml(html, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
        {
            HtmlUtility.GetBookmarks(ref token, bookmarks);
            HtmlUtility.AddLinkType(errors, file, ref token, _config.TrustedDomains, _locale, addLocale: true);

            if (token.Type == HtmlTokenType.Text)
            {
                searchText.Append(token.RawText);
            }
        });

        _bookmarkValidator?.AddBookmarks(file, bookmarks);
        _searchIndexBuilder?.SetBody(file, searchText.ToString());

        return LocalizationUtility.AddLeftToRightMarker(_cultureInfo, result);
    }

    private string CreateContent(FilePath file, string? mime, ref JObject pageModel)
    {
        if (JsonSchemaProvider.IsConceptual(mime))
        {
            var conceptual = JsonUtility.ToObject<ConceptualModel>(ErrorBuilder.Null, pageModel)!;
            ProcessConceptualHtml(pageModel.Value<string>("conceptual") ?? "", conceptual);
            pageModel = JsonUtility.SortProperties(_config.DryRun ? new JObject() : JsonUtility.ToJObject(conceptual));
            return conceptual.Conceptual ?? "";
        }
        else if (JsonSchemaProvider.IsLandingData(mime))
        {
            return ProcessHtml(_errors, file, pageModel.Value<string>("conceptual") ?? "");
        }

        // Generate SDP content
        var model = RunJavaScript($"{mime}.html.primary.js", pageModel);
        var content = RunMustache(_errors, $"{mime}.html", model);

        return ProcessHtml(_errors, file, content);
    }

    private void ProcessConceptualHtml(string html, ConceptualModel conceptual)
    {
        var wordCount = 0L;

        var result = HtmlUtility.TransformHtml(html, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
        {
            HtmlUtility.AddLocaleIfMissingForAbsolutePath(ref token, _locale);

            if (token.Type == HtmlTokenType.Text)
            {
                if (!_config.DryRun)
                {
                    wordCount += WordCount.CountWord(token.RawText.Span);
                }
            }
        });

        conceptual.Conceptual = LocalizationUtility.AddLeftToRightMarker(_cultureInfo, result);
        conceptual.WordCount = wordCount;
    }

    private JObject LoadGlobalTokens(ErrorBuilder errors)
    {
        var defaultTokens = _package.TryLoadYamlOrJson<JObject>(errors, "ContentTemplate/token");
        var localeTokens = _package.TryLoadYamlOrJson<JObject>(errors, $"ContentTemplate/token.{_locale}");
        if (defaultTokens == null)
        {
            return localeTokens ?? new JObject();
        }
        JsonUtility.Merge(defaultTokens, localeTokens);
        return defaultTokens;
    }
}
