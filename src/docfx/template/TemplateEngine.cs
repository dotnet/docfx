// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine
    {
        public (HashSet<string> htmlMetaHidden, Dictionary<string, string> htmlMetaNames) HtmlMetaConfigs { get; }

        private const string DefaultTemplateDir = "_themes";
        private static readonly string[] s_resourceFolders = new[] { "global", "css", "fonts" };

        private readonly string _templateDir;
        private readonly JObject _global;
        private readonly LiquidTemplate _liquid;
        private readonly JavascriptEngine _js;
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

            HtmlMetaConfigs = (
                metadataSchema.HtmlMetaHidden.ToHashSet(),
                metadataSchema.Properties
                .Where(prop => !string.IsNullOrEmpty(prop.Value.HtmlMetaName))
                .ToDictionary(prop => prop.Key, prop => prop.Value.HtmlMetaName));
        }

        public bool IsPage(string mime)
        {
            return mime == null || !_schemas.TryGetValue(mime, out var schemaTemplate) || schemaTemplate.Value.IsPage;
        }

        public TemplateSchema GetSchema(SourceInfo<string> schemaName)
        {
            return !string.IsNullOrEmpty(schemaName) && _schemas.TryGetValue(schemaName, out var schemaTemplate)
               ? schemaTemplate.Value
               : throw Errors.SchemaNotFound(schemaName).ToException();
        }

        public string RunLiquid(string content, Document file, JObject rawMetadata)
        {
            // TODO: only works for conceptual
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

        public string Render(string templateName, JObject pageModel)
        {
            // TODO: run mustache
            throw new NotImplementedException();
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

        public JObject RunJint(string scriptName, JObject model, string methodName = "transform")
        {
            var scriptPath = Path.Combine(_templateDir, "ContentTemplate", scriptName);
            if (File.Exists(scriptPath))
            {
                return JObject.Parse(((JObject)_js.Run(scriptPath, methodName, model)).Value<string>("content"));
            }
            return model;
        }

        public static bool IsLandingData(string mime)
        {
            return mime != null && string.Equals(typeof(LandingData).Name, mime, StringComparison.OrdinalIgnoreCase);
        }

        public string GetToken(string key)
        {
            return _global[key]?.ToString();
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

        public static JObject CreateMetadata(JObject rawMetadata)
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

        private JObject LoadGlobalTokens(string contentTemplateDir)
        {
            var path = Path.Combine(contentTemplateDir, "token.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
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
    }
}
