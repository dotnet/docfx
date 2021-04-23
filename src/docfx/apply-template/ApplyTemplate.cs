// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlReaderWriter;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class ApplyTemplate
    {
        public static bool Run(CommandLineOptions options)
        {
            using var errors = new ErrorWriter(options.Log);
            if (!string.IsNullOrEmpty(options.Template))
            {
                if (string.IsNullOrEmpty(options.Directory))
                {
                    errors.Add(Errors.ApplyTemplate.StructuredJsonDirNotSpecified());
                    return errors.HasError;
                }
                var package = new LocalPackage(options.Template); // path to template
                var templateEngine = new TemplateEngine(errors, options.Locale ?? "en-us", package);

                Parallel.ForEach(
                    Directory.GetFiles(options.Directory!),
                    filePath =>
                    {
                        var file = new FilePath(filePath);
                        var pageModel = JsonUtility.Deserialize<JObject>(errors, File.ReadAllText(file.Path), file);
                        var schema = pageModel.GetValue("schema");
                        var (model, _) = CreateTemplateModel(
                            errors,
                            file,
                            templateEngine,
                            pageModel,
                            schema is null ? string.Empty : schema.ToString(),
                            options.Locale ?? "en-us");
                        File.WriteAllText(Path.ChangeExtension(filePath, "raw.page.json"), JsonUtility.Serialize(model));
                    });
            }
            else
            {
                errors.Add(Errors.ApplyTemplate.TemplateNotSpecified());
            }

            return errors.HasError;
        }

        private static (TemplateModel model, JObject metadata) CreateTemplateModel(
            ErrorBuilder errors, FilePath file, TemplateEngine templateEngine, JObject pageModel, string? mime, string locale)
        {
            var content = CreateContent(errors, file, templateEngine, mime, locale, pageModel);

            if (string.IsNullOrEmpty(content))
            {
                content = "<div></div>";
            }

            var jsName = $"{mime}.mta.json.js";
            var templateMetadata = templateEngine.RunJavaScript(jsName, pageModel) as JObject ?? new JObject();

            if (TemplateEngine.IsLandingData(mime))
            {
                templateMetadata.Remove("conceptual");
            }

            var metadata = new JObject(templateMetadata.Properties().Where(p => !p.Name.StartsWith("_")))
            {
                ["is_dynamic_rendering"] = true,
            };

            var pageMetadata = HtmlUtility.CreateHtmlMetaTags(metadata);

            // content for *.raw.page.json
            var model = new TemplateModel(content, templateMetadata, pageMetadata, "_themes/");

            return (model, metadata);
        }

        private static string CreateContent(
            ErrorBuilder errors, FilePath file, TemplateEngine templateEngine, string? mime, string locale, JObject pageModel)
        {
            if (TemplateEngine.IsConceptual(mime) || TemplateEngine.IsLandingData(mime))
            {
                // Conceptual and Landing Data
                return pageModel.Value<string>("conceptual") ?? "";
            }

            // Generate SDP content
            var model = templateEngine.RunJavaScript($"{mime}.html.primary.js", pageModel);
            var content = templateEngine.RunMustache(errors, $"{mime}.html", model);

            return ProcessHtml(errors, file, content, locale);
        }

        private static string ProcessHtml(ErrorBuilder errors, FilePath file, string html, string locale)
        {
            var searchText = new StringBuilder();

            var result = HtmlUtility.TransformHtml(html, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
            {
                var trustedDomains = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data/docs/trusted-domains.json")));
                HtmlUtility.AddLinkType(errors, file, ref token, locale, JsonUtility.ToObject<Dictionary<string, TrustedDomains>>(errors, trustedDomains));
            });
            return AddLeftToRightMarker(result, locale);
        }

        private static string AddLeftToRightMarker(string text, string locale)
        {
            var cultureInfo = BuildOptions.CreateCultureInfo(locale);
            if (cultureInfo.TextInfo.IsRightToLeft)
            {
                var adjustment = new Regex(@"(^|\s|\>)(C#|F#|C\+\+)(\s*|[.!?;:]*)(\<|[\n\r]|$)", RegexOptions.IgnoreCase);
                return adjustment.Replace(text, me => $"{me.Groups[1]}{me.Groups[2]}&lrm;{me.Groups[3]}{me.Groups[4]}");
            }
            else
            {
                return text;
            }
        }
    }
}
