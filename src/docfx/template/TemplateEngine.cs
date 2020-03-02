// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine
    {
        private static readonly string[] s_resourceFolders = new[] { "global", "css", "fonts" };

        private readonly string _templateDir;
        private readonly string _contentTemplateDir;
        private readonly JObject _global;
        private readonly LiquidTemplate _liquid;
        private readonly IJavaScriptEngine _js;
        private readonly IReadOnlyDictionary<string, Lazy<TemplateSchema>> _schemas;
        private readonly MustacheTemplate _mustacheTemplate;

        public TemplateEngine(string docsetPath, Config config, string locale, PackageResolver packageResolver)
        {
            _templateDir = config.Template.Type switch
            {
                PackageType.None => Path.Combine(docsetPath, "_themes"),
                _ => packageResolver.ResolvePackage(LocalizationUtility.GetLocalizedTheme(config.Template, locale, config.DefaultLocale), PackageFetchOptions.DepthOne),
            };

            _contentTemplateDir = Path.Combine(_templateDir, "ContentTemplate");
            var schemaDir = Path.Combine(_contentTemplateDir, "schemas");

            _global = LoadGlobalTokens();
            _schemas = LoadSchemas(schemaDir, _contentTemplateDir);
            _liquid = new LiquidTemplate(_contentTemplateDir);

            // TODO: remove JINT after Microsoft.CharkraCore NuGet package
            // supports linux and macOS: https://github.com/microsoft/ChakraCore/issues/2578
            _js = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? (IJavaScriptEngine)new ChakraCoreJsEngine(_contentTemplateDir, _global)
                : new JintJsEngine(_contentTemplateDir, _global);

            _mustacheTemplate = new MustacheTemplate(_contentTemplateDir);
        }

        public bool IsPage(string? mime)
        {
            return mime is null || !_schemas.TryGetValue(mime, out var schemaTemplate) || schemaTemplate.Value.IsPage;
        }

        public TemplateSchema GetSchema(SourceInfo<string> schemaName)
        {
            return !string.IsNullOrEmpty(schemaName) && _schemas.TryGetValue(schemaName, out var schemaTemplate)
               ? schemaTemplate.Value
               : throw Errors.SchemaNotFound(schemaName).ToException();
        }

        public string RunLiquid(Document file, TemplateModel model)
        {
            var layout = model.RawMetadata?.Value<string>("layout") ?? "";
            var themeRelativePath = PathUtility.GetRelativePathToFile(file.SitePath, "_themes");

            var liquidModel = new JObject
            {
                ["content"] = model.Content,
                ["page"] = model.RawMetadata,
                ["metadata"] = model.PageMetadata,
                ["theme_rel"] = themeRelativePath,
            };

            return _liquid.Render(layout, liquidModel);
        }

        public string RunMustache(string templateName, JObject pageModel)
        {
            return _mustacheTemplate.Render(templateName, pageModel);
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
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFilePath)));
                        File.Copy(file, outputFilePath, overwrite: true);
                    });
                }
            }
        }

        public JObject RunJint(string scriptName, JObject model, string methodName = "transform", bool tryParseFromContent = true)
        {
            var scriptPath = Path.Combine(_contentTemplateDir, scriptName);
            if (!File.Exists(scriptPath))
            {
                return model;
            }

            var jsResult = _js.Run(scriptPath, methodName, model);

            var result = new JObject();
            if (jsResult is JValue)
            {
                // workaround for result is not JObject
                result["content"] = jsResult;
                return result;
            }

            result = (JObject)_js.Run(scriptPath, methodName, model);
            if (tryParseFromContent
                && result.TryGetValue<JValue>("content", out var value)
                && value.Value is string content)
            {
                try
                {
                    return JObject.Parse(content);
                }
                catch
                {
                    return result;
                }
            }
            return result;
        }

        public static bool IsLandingData(string? mime)
        {
            return mime != null && string.Equals(typeof(LandingData).Name, mime, StringComparison.OrdinalIgnoreCase);
        }

        public string? GetToken(string key)
        {
            return _global[key]?.ToString();
        }

        private JObject LoadGlobalTokens()
        {
            var path = Path.Combine(_contentTemplateDir, "token.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
        }

        private static IReadOnlyDictionary<string, Lazy<TemplateSchema>>
            LoadSchemas(string schemaDir, string contentTemplateDir)
        {
            var schemas = Directory.Exists(schemaDir)
                ? (from k in Directory.EnumerateFiles(schemaDir, "*.schema.json", SearchOption.TopDirectoryOnly)
                   let fileName = Path.GetFileName(k)
                   select fileName.Substring(0, fileName.Length - ".schema.json".Length))
                   .ToDictionary(schemaName => schemaName, schemaName => new Lazy<TemplateSchema>(() => new TemplateSchema(schemaName, schemaDir, contentTemplateDir)))
                : new Dictionary<string, Lazy<TemplateSchema>>();

            schemas.Add("LandingData", new Lazy<TemplateSchema>(() => new TemplateSchema("LandingData", schemaDir, contentTemplateDir)));
            return schemas;
        }
    }
}
