// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine
    {
        private const string DefaultTemplateDir = "_themes";
        private static readonly string[] s_resourceFolders = new[] { "global", "css", "fonts" };

        private readonly string _templateDir;
        private readonly JObject _global;
        private readonly LiquidTemplate _liquid;
        private readonly JavascriptEngine _js;
        private readonly HashSet<string> _htmlMetaHidden;
        private readonly Dictionary<string, string> _htmlMetaNames;
        private readonly IReadOnlyDictionary<string, Lazy<TemplateSchema>> _schemas;

        private TemplateEngine(string templateDir, JsonSchema metadataSchema)
        {
            var contentTemplateDir = Path.Combine(templateDir, "ContentTemplate");
            var schemaDir = Path.Combine(contentTemplateDir, "schemas");

            _global = LoadGlobalTokens(contentTemplateDir);
            _schemas = LoadSchemas(schemaDir, contentTemplateDir);
            _templateDir = templateDir;
            _liquid = new LiquidTemplate(templateDir);
            _js = new JavascriptEngine(contentTemplateDir, _global);

            _htmlMetaHidden = metadataSchema.HtmlMetaHidden.ToHashSet();
            _htmlMetaNames = metadataSchema.Properties
                .Where(prop => !string.IsNullOrEmpty(prop.Value.HtmlMetaName))
                .ToDictionary(prop => prop.Key, prop => prop.Value.HtmlMetaName);
        }

        public bool IsData(string mime)
        {
            if (mime != null && _schemas.TryGetValue(mime, out var schemaTemplate) && schemaTemplate.Value.IsData)
            {
                return true;
            }

            return false;
        }

        public TemplateSchema GetJsonSchema(string schemaName)
        {
            return !string.IsNullOrEmpty(schemaName) && _schemas.TryGetValue(schemaName, out var schemaTemplate) ? schemaTemplate.Value : default;
        }

        public string Render(string content, Document file, JObject rawMetadata, string mime)
        {
            // TODO: only works for conceptual
            rawMetadata = TransformPageMetadata(rawMetadata, mime);
            var metadata = CreateMetadata(rawMetadata);

            var layout = rawMetadata.Value<string>("layout") ?? "";
            var themeRelativePath = PathUtility.GetRelativePathToFile(file.SitePath, "_themes");

            var liquidModel = new JObject
            {
                ["content"] = content,
                ["page"] = rawMetadata,
                ["metadata"] = metadata,
                ["theme_rel"] = themeRelativePath,
            };

            return _liquid.Render(layout, liquidModel);
        }

        public (TemplateModel model, JObject metadata) TransformToTemplateModel(string conceptual, JObject rawMetadata, string mime)
        {
            rawMetadata = TransformPageMetadata(rawMetadata, mime);
            var metadata = CreateMetadata(rawMetadata);
            var pageMetadata = CreateHtmlMetaTags(metadata);

            var model = new TemplateModel
            {
                Content = conceptual,
                RawMetadata = rawMetadata,
                PageMetadata = pageMetadata,
                ThemesRelativePathToOutputRoot = "_themes/",
            };

            return (model, metadata);
        }

        public void CopyTo(string outputPath)
        {
            foreach (var resourceDir in s_resourceFolders)
            {
                var srcDir = Path.Combine(_templateDir, resourceDir);
                if (Directory.Exists(srcDir))
                {
                    Parallel.ForEach(Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories), file =>
                    {
                        var outputFilePath = Path.Combine(outputPath, "_themes", file.Substring(_templateDir.Length + 1));
                        PathUtility.CreateDirectoryFromFilePath(outputFilePath);
                        File.Copy(file, outputFilePath, overwrite: true);
                    });
                }
            }
        }

        public string GetToken(string key)
        {
            return _global[key]?.ToString();
        }

        public JObject TransformTocMetadata(object model)
            => TransformMetadata("toc.json.js", JsonUtility.ToJObject(model));

        public static bool IsLandingData(string mime)
        {
            if (mime != null)
            {
                return string.Equals(typeof(LandingData).Name, mime, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public static TemplateEngine Create(Docset docset)
        {
            Debug.Assert(docset != null);

            if (string.IsNullOrEmpty(docset.Config.Template))
            {
                return new TemplateEngine(Path.Combine(docset.DocsetPath, DefaultTemplateDir), new JsonSchema());
            }

            var (themeRemote, themeBranch) = LocalizationUtility.GetLocalizedTheme(docset.Config.Template, docset.Locale, docset.Config.Localization.DefaultLocale);
            var (themePath, themeRestoreMap) = docset.RestoreMap.GetGitRestorePath(themeRemote, themeBranch, docset.DocsetPath);
            Log.Write($"Using theme '{themeRemote}#{themeRestoreMap.DependencyLock?.Commit}' at '{themePath}'");

            return new TemplateEngine(themePath, docset.MetadataSchema);
        }

        private JObject TransformPageMetadata(JObject rawMetadata, string mime)
        {
            // TODO: transform based on mime
            rawMetadata = TransformMetadata("Conceptual.mta.json.js", rawMetadata);

            if (IsLandingData(mime))
            {
                rawMetadata["_op_layout"] = "LandingPage";
                rawMetadata["layout"] = "LandingPage";
                rawMetadata["page_type"] = "landingdata";

                rawMetadata.Remove("_op_gitContributorInformation");
                rawMetadata.Remove("_op_allContributorsStr");
            }

            return RemoveUpdatedAtDateTime(rawMetadata);
        }

        private JObject LoadGlobalTokens(string contentTemplateDir)
        {
            var path = Path.Combine(contentTemplateDir, "token.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
        }

        private JObject TransformMetadata(string scriptPath, JObject model)
        {
            return JObject.Parse(((JObject)_js.Run(scriptPath, "transform", model)).Value<string>("content"));
        }

        private static IReadOnlyDictionary<string, Lazy<TemplateSchema>>
            LoadSchemas(string schemaDir, string contentTemplateDir)
        {
            var schemas = Directory.Exists(schemaDir) ? (from k in Directory.EnumerateFiles(schemaDir, "*.schema.json", SearchOption.TopDirectoryOnly)
                                                         let fileName = Path.GetFileName(k)
                                                         select fileName.Substring(0, fileName.Length - ".schema.json".Length))
                                                         .ToDictionary(schemaName => schemaName, schemaName => new Lazy<TemplateSchema>(() => new TemplateSchema(schemaName, schemaDir, contentTemplateDir)))
                                                         : new Dictionary<string, Lazy<TemplateSchema>>();

            schemas.Add("LandingData", new Lazy<TemplateSchema>(() => new TemplateSchema("LandingData", schemaDir, contentTemplateDir)));
            return schemas;
        }

        private static JObject CreateMetadata(JObject rawMetadata)
        {
            var metadata = new JObject();

            foreach (var (key, value) in rawMetadata)
            {
                if (!key.StartsWith("_"))
                {
                    metadata[key] = value;
                }
            }

            metadata["is_dynamic_rendering"] = true;

            return metadata;
        }

        private string CreateHtmlMetaTags(JObject metadata)
        {
            var result = new StringBuilder();

            foreach (var property in metadata.Properties().OrderBy(p => p.Name))
            {
                var key = property.Name;
                var value = property.Value;
                if (value is JObject || _htmlMetaHidden.Contains(key))
                {
                    continue;
                }

                var content = "";
                var name = _htmlMetaNames.TryGetValue(key, out var diplayName) ? diplayName : key;

                if (value is JArray arr)
                {
                    foreach (var v in value)
                    {
                        if (v is JValue)
                        {
                            result.AppendLine($"<meta name=\"{HttpUtility.HtmlEncode(name)}\" content=\"{HttpUtility.HtmlEncode(v)}\" />");
                        }
                    }
                    continue;
                }
                else if (value.Type == JTokenType.Boolean)
                {
                    content = (bool)value ? "true" : "false";
                }
                else
                {
                    content = value.ToString();
                }

                result.AppendLine($"<meta name=\"{HttpUtility.HtmlEncode(name)}\" content=\"{HttpUtility.HtmlEncode(content)}\" />");
            }

            return result.ToString();
        }

        private static JObject RemoveUpdatedAtDateTime(JObject rawMetadata)
        {
            JToken gitContributorInformation;
            if (rawMetadata.TryGetValue("_op_gitContributorInformation", out gitContributorInformation)
                && ((JObject)gitContributorInformation).ContainsKey("updated_at_date_time"))
            {
                ((JObject)rawMetadata["_op_gitContributorInformation"]).Remove("updated_at_date_time");
            }
            return rawMetadata;
        }
    }
}
