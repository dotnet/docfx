// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HtmlReaderWriter;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class TemplateModelBuilder
    {
        public static (TemplateModel model, JObject metadata) CreateTemplateModel(
            ErrorBuilder errors,
            TemplateEngineRunner templateEngineRunner,
            FilePath file,
            JObject pageModel,
            string? schema,
            string locale,
            CultureInfo? cultureInfo,
            Dictionary<string, TrustedDomains> trustedDomains,
            bool dryRun = false,
            BookmarkValidator? bookmarkValidator = null,
            SearchIndexBuilder? searchIndexBuilder = null)
        {
            var content = CreateContent(
                templateEngineRunner, errors, file, schema, locale, cultureInfo, pageModel, trustedDomains, bookmarkValidator, searchIndexBuilder);

            if (dryRun)
            {
                return (new TemplateModel("", new JObject(), "", ""), new JObject());
            }

            // Hosting layers treats empty content as 404, so generate an empty <div></div>
            if (string.IsNullOrWhiteSpace(content))
            {
                content = "<div></div>";
            }

            var jsName = $"{schema}.mta.json.js";
            var templateMetadata = templateEngineRunner.RunJavaScript(jsName, pageModel) as JObject ?? new JObject();

            if (TemplateEngine.IsLandingData(schema))
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

            return LocalizationUtility.AddLeftToRightMarker(cultureInfo ?? BuildOptions.CreateCultureInfo(locale), result);
        }

        private static string CreateContent(
            TemplateEngineRunner templateEngineRunner,
            ErrorBuilder errors,
            FilePath file,
            string? schema,
            string locale,
            CultureInfo? cultureInfo,
            JObject pageModel,
            Dictionary<string, TrustedDomains> trustedDomains,
            BookmarkValidator? bookmarkValidator = null,
            SearchIndexBuilder? searchIndexBuilder = null)
        {
            if (TemplateEngine.IsConceptual(schema) || TemplateEngine.IsLandingData(schema))
            {
                // Conceptual and Landing Data
                return pageModel.Value<string>("conceptual") ?? "";
            }

            // Generate SDP content
            var model = templateEngineRunner.RunJavaScript($"{schema}.html.primary.js", pageModel);
            var content = templateEngineRunner.RunMustache(errors, $"{schema}.html", model);

            return ProcessHtml(errors, file, content, trustedDomains, locale, cultureInfo, bookmarkValidator, searchIndexBuilder);
        }
    }
}
