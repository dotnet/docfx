// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlReaderWriter;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine
    {
        private readonly Package _package;
        private readonly Lazy<TemplateDefinition> _templateDefinition;
        private readonly JObject _global;
        private readonly LiquidTemplate _liquid;
        private readonly ThreadLocal<JavaScriptEngine> _js;
        private readonly MustacheTemplate _mustacheTemplate;
        private readonly BuildOptions _buildOptions;

        public TemplateEngine(ErrorBuilder errors, Config config, PackageResolver packageResolver, BuildOptions buildOptions)
        {
            _buildOptions = buildOptions;

            var template = config.Template;
            var templateFetchOptions = PackageFetchOptions.DepthOne;
            if (template.Type == PackageType.None)
            {
                template = new("_themes");
                templateFetchOptions |= PackageFetchOptions.IgnoreDirectoryNonExistedError;
            }

            _package = packageResolver.ResolveAsPackage(template, templateFetchOptions);

            _templateDefinition = new(() => _package.TryLoadYamlOrJson<TemplateDefinition>(errors, "template") ?? new());

            _global = LoadGlobalTokens(errors);

            _liquid = new(_package, config.TemplateBasePath, _global);
            _js = new(() => JavaScriptEngine.Create(_package, _global));
            _mustacheTemplate = new(_package, "ContentTemplate", _global);
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
                    return JToken.Parse(content);
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

        public (TemplateModel model, JObject metadata) CreateTemplateModel(
            ErrorBuilder errors,
            FilePath file,
            string? mime,
            JObject pageModel,
            string locale,
            CultureInfo? cultureInfo,
            Dictionary<string, TrustedDomains> trustedDomains,
            BookmarkValidator? bookmarkValidator = null,
            SearchIndexBuilder? searchIndexBuilder = null,
            bool dryRun = false)
        {
            var content = CreateContent(errors, file, mime, pageModel, locale, cultureInfo, trustedDomains, bookmarkValidator, searchIndexBuilder);

            if (dryRun)
            {
                return (new TemplateModel("", new JObject(), "", ""), new JObject());
            }

            // Hosting layers treats empty content as 404, so generate an empty <div></div>
            if (string.IsNullOrWhiteSpace(content))
            {
                content = "<div></div>";
            }

            var jsName = $"{mime}.mta.json.js";
            var templateMetadata = RunJavaScript(jsName, pageModel) as JObject ?? new JObject();

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

        public static string ProcessHtml(
            ErrorBuilder errors,
            FilePath file,
            string html,
            Dictionary<string, TrustedDomains> trustedDomains,
            string locale,
            CultureInfo? cultureInfo,
            BookmarkValidator? bookmarkValidator,
            SearchIndexBuilder? searchIndexBuilder)
        {
            var bookmarks = new HashSet<string>();
            var searchText = new StringBuilder();

            var result = HtmlUtility.TransformHtml(html, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
            {
                HtmlUtility.GetBookmarks(ref token, bookmarks);
                HtmlUtility.AddLinkType(errors, file, ref token, locale, trustedDomains);

                if (token.Type == HtmlTokenType.Text)
                {
                    searchText.Append(token.RawText);
                }
            });

            bookmarkValidator?.AddBookmarks(file, bookmarks);
            searchIndexBuilder?.SetBody(file, searchText.ToString());

            return LocalizationUtility.AddLeftToRightMarker(cultureInfo ?? LocalizationUtility.CreateCultureInfo(locale), result);
        }

        private string CreateContent(
            ErrorBuilder errors,
            FilePath file,
            string? mime,
            JObject pageModel,
            string locale,
            CultureInfo? cultureInfo,
            Dictionary<string, TrustedDomains> trustedDomains,
            BookmarkValidator? bookmarkValidator = null,
            SearchIndexBuilder? searchIndexBuilder = null)
        {
            if (JsonSchemaProvider.IsConceptual(mime) || JsonSchemaProvider.IsLandingData(mime))
            {
                // Conceptual and Landing Data
                return pageModel.Value<string>("conceptual") ?? "";
            }

            // Generate SDP content
            var model = RunJavaScript($"{mime}.html.primary.js", pageModel);
            var content = RunMustache(errors, $"{mime}.html", model);

            return ProcessHtml(errors, file, content, trustedDomains, locale, cultureInfo, bookmarkValidator, searchIndexBuilder);
        }

        private JObject LoadGlobalTokens(ErrorBuilder errors)
        {
            var defaultTokens = _package.TryLoadYamlOrJson<JObject>(errors, "ContentTemplate/token");
            var localeTokens = _package.TryLoadYamlOrJson<JObject>(errors, $"ContentTemplate/token.{_buildOptions.Locale}");
            if (defaultTokens == null)
            {
                return localeTokens ?? new JObject();
            }
            JsonUtility.Merge(defaultTokens, localeTokens);
            return defaultTokens;
        }
    }
}
