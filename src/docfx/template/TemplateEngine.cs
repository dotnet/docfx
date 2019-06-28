// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core.Tokens;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine
    {
        private const string DefaultTemplateDir = "_themes";
        private static readonly string[] s_resourceFolders = new[] { "global", "css", "fonts" };

        private readonly string _templateDir;
        private readonly LiquidTemplate _liquid;
        private readonly JavascriptEngine _js;
        private readonly HashSet<string> _htmlMetaHidden;
        private readonly Dictionary<string, string> _htmlMetaNames;
        private readonly IReadOnlyDictionary<string, (bool isData, bool transformMetadata, (JsonSchemaValidator, JsonSchemaTransformer) jsonSchema)> _schemas;

        public JObject Global { get; }

        private TemplateEngine(string templateDir, JsonSchema metadataSchema)
        {
            var contentTemplateDir = Path.Combine(templateDir, "ContentTemplate");
            var schemaDir = Path.Combine(contentTemplateDir, "schemas");

            Global = LoadGlobalTokens(contentTemplateDir);
            _schemas = LoadSchemas(schemaDir, contentTemplateDir);
            _templateDir = templateDir;
            _liquid = new LiquidTemplate(templateDir);
            _js = new JavascriptEngine(contentTemplateDir);

            _htmlMetaHidden = metadataSchema.HtmlMetaHidden.ToHashSet();
            _htmlMetaNames = metadataSchema.Properties
                .Where(prop => !string.IsNullOrEmpty(prop.Value.HtmlMetaName))
                .ToDictionary(prop => prop.Key, prop => prop.Value.HtmlMetaName);
        }

        public bool IsData(string mime)
        {
            if (mime != null && _schemas.TryGetValue(mime, out var schema) && schema.isData)
            {
                return true;
            }

            return false;
        }

        public static bool IsLandingData(string mime)
        {
            if (mime != null)
            {
                return string.Equals(typeof(LandingData).Name, mime, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public (JsonSchemaValidator, JsonSchemaTransformer) GetJsonSchema(string schemaName)
        {
            return !string.IsNullOrEmpty(schemaName) && _schemas.TryGetValue(schemaName, out var schema) ? schema.jsonSchema : default;
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

        public string GenerateHtml(JToken xxx)
        {
            // TODO: run JINT + mustache
            throw new NotImplementedException();
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
            return Global[key]?.ToString();
        }

        public JObject PreprocessMetadata(JObject outputMetadata, Document file)
        {
            var docset = file.Docset;

            var processedMetadata = (JObject)JsonUtility.DeepClone(outputMetadata);
            processedMetadata["search.ms_docsetname"] = docset.Config.Name;
            processedMetadata["search.ms_product"] = docset.Config.Product;
            processedMetadata["search.ms_sitename"] = "Docs";

            processedMetadata["__global"] = Global;

            return processedMetadata;
        }

        public JObject TransformTocMetadata(object model)
            => TransformMetadata("toc.json.js", JsonUtility.ToJObject(model));

        private IReadOnlyDictionary<string, (bool isData, bool transformMetadata, (JsonSchemaValidator, JsonSchemaTransformer) jsonSchema)>
            LoadSchemas(string schemaDir, string contentTemplateDir)
        {
            var schemas = new Dictionary<string, (bool isData, bool transformMetadata, (JsonSchemaValidator, JsonSchemaTransformer) jsonSchema)>();
            var schemaNames = Directory.Exists(schemaDir) ? Directory.EnumerateFiles(schemaDir, "*.schema.json", SearchOption.TopDirectoryOnly)
                                                    .Select(k => Path.GetFileNameWithoutExtension(k))
                                                    .Select(k => k.Substring(0, k.Length - ".schema".Length)).ToHashSet() : new HashSet<string>();
            var allJsFiles = Directory.Exists(contentTemplateDir) ? Directory.EnumerateFiles(contentTemplateDir, "*.js", SearchOption.TopDirectoryOnly)
                             .Select(k => Path.GetFileNameWithoutExtension(k))
                             .GroupBy(k => Regex.Replace(k, "(\\.html\\.primary|\\.mta\\.json)$", ""))
                             .ToDictionary(g => g.Key, g => g.ToList()) : default;
            foreach (var schemaName in schemaNames)
            {
                bool isData = true;
                bool transformMetadata = false;
                if (allJsFiles.TryGetValue(schemaName, out var jsFiles))
                {
                    foreach (var jsFile in jsFiles)
                    {
                        if (jsFile.EndsWith(".html.primary"))
                            isData = false;
                        if (jsFile.EndsWith(".mta.json"))
                            transformMetadata = true;
                    }
                }
                schemas.Add(schemaName, (isData, transformMetadata, new Lazy<(JsonSchemaValidator, JsonSchemaTransformer)>(() => GetJsonSchemaCore(schemaName)).Value));
            }
            schemas.Add("LandingData", (false, false, new Lazy<(JsonSchemaValidator, JsonSchemaTransformer)>(() => GetJsonSchemaCore("LandingData")).Value));
            return schemas;

            (JsonSchemaValidator, JsonSchemaTransformer) GetJsonSchemaCore(string schemaName)
            {
                if (schemaName is null)
                {
                    return default;
                }

                var schemaFilePath = Path.Combine(schemaDir, $"{schemaName}.schema.json");
                if (string.Equals(schemaName, "LandingData", StringComparison.OrdinalIgnoreCase))
                {
                    schemaFilePath = Path.Combine(AppContext.BaseDirectory, "data", "schemas", "LandingData.json");
                }
                if (!File.Exists(schemaFilePath))
                {
                    return default;
                }

                var jsonSchema = JsonUtility.Deserialize<JsonSchema>(File.ReadAllText(schemaFilePath), schemaFilePath);
                return (new JsonSchemaValidator(jsonSchema), new JsonSchemaTransformer(jsonSchema));
            }
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

        private static JObject ToJObject(Contributor info)
        {
            return new JObject
            {
                ["display_name"] = !string.IsNullOrEmpty(info.DisplayName) ? info.DisplayName : info.Name,
                ["id"] = info.Id,
                ["profile_url"] = info.ProfileUrl,
            };
        }
    }
}
