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
            => mime != null && _schemas.TryGetValue(mime, out var schemaTemplate) && schemaTemplate.Value.IsData;

        public TemplateSchema GetJsonSchema(string schemaName)
            => !string.IsNullOrEmpty(schemaName) && _schemas.TryGetValue(schemaName, out var schemaTemplate) ? schemaTemplate.Value : default;

        public string RunLiquid(string content, Document file, JObject rawMetadata, string mime)
        {
            // TODO: only works for conceptual
            rawMetadata = TransformPageMetadata(rawMetadata, mime);
            var metadata = TemplateUtility.CreateMetadata(rawMetadata);

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

        public string RenderMustache(string mime, JObject pageModel)
        {
            // TODO: run JINT + mustache
            throw new NotImplementedException();
        }

        public (TemplateModel model, JObject metadata) TransformToTemplateModel(string conceptual, JObject rawMetadata, string mime)
        {
            rawMetadata = TransformPageMetadata(rawMetadata, mime);
            var metadata = TemplateUtility.CreateMetadata(rawMetadata);
            var pageMetadata = TemplateUtility.CreateHtmlMetaTags(metadata, _htmlMetaHidden, _htmlMetaNames);

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
            => RunJintTransform("toc.json.js", JsonUtility.ToJObject(model));

        public JObject TransformData(Document file, JObject pageModel)
        {
            Debug.Assert(file.IsData);

            _ = _schemas.TryGetValue(file.Mime, out var templateSchema);
            Debug.Assert(templateSchema.Value != null);

            if (templateSchema.Value.HasDataTransformJs)
            {
                return RunJintTransform(templateSchema.Value.DataTransformJsPath, pageModel);
            }
            return pageModel;
        }

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
            rawMetadata = RunJintTransform("Conceptual.mta.json.js", rawMetadata);

            if (IsLandingData(mime))
            {
                rawMetadata["_op_layout"] = "LandingPage";
                rawMetadata["layout"] = "LandingPage";
                rawMetadata["page_type"] = "landingdata";

                rawMetadata.Remove("_op_gitContributorInformation");
                rawMetadata.Remove("_op_allContributorsStr");
            }

            return TemplateUtility.RemoveUpdatedAtDateTime(rawMetadata);
        }

        private JObject LoadGlobalTokens(string contentTemplateDir)
        {
            var path = Path.Combine(contentTemplateDir, "token.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
        }

        private JObject RunJintTransform(string scriptPath, JObject model)
        {
            return JObject.Parse(((JObject)_js.Run(scriptPath, "transform", model)).Value<string>("content"));
        }

        private string RunMustache(string scriptPath, JObject model)
        {
            throw new NotImplementedException();
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
