// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class LegacyTemplate
    {
        private static readonly string[] s_resourceFolders = new[] { "global", "css", "fonts" };

        private readonly string _templateDir;
        private readonly string _locale;
        private readonly LiquidTemplate _liquid;
        private readonly JavaScript _js;

        public JObject Global { get; }

        public LegacyTemplate(string templateDir, string locale)
        {
            var contentTemplateDir = Path.Combine(templateDir, "ContentTemplate");

            _templateDir = templateDir;
            _locale = locale.ToLowerInvariant();
            _liquid = new LiquidTemplate(templateDir);
            _js = new JavaScript(contentTemplateDir);
            Global = LoadGlobalTokens(templateDir, _locale);
        }

        public string Render(PageModel model, Document file)
        {
            // TODO: only works for conceptual
            var content = model.Content.ToString();
            var page = LegacyMetadata.GenerateLegacyRawMetadata(model, content, file);
            var metadata = LegacyMetadata.CreateHtmlMetaTags(page);
            var layout = page.Value<string>("layout");
            var themeRelativePath = PathUtility.GetRelativePathToFile(file.SitePath, "_themes");

            var liquidModel = new JObject
            {
                ["content"] = content,
                ["page"] = page,
                ["metadata"] = metadata,
                ["theme_rel"] = themeRelativePath,
            };

            return _liquid.Render(layout, liquidModel);
        }

        public JObject TransformMetadata(string schemaName, JObject metadata)
        {
            return _js.Run($"{schemaName}.mta.json.js", metadata);
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
                        PathUtility.CreateDirectoryIfNotEmpty(Path.GetDirectoryName(outputFilePath));
                        File.Copy(file, outputFilePath, overwrite: true);
                    });
                }
            }
        }

        private JObject LoadGlobalTokens(string templateDir, string locale)
        {
            var path = Path.Combine(templateDir, $"LocalizedTokens/docs({locale}).html/tokens.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
        }
    }
}
