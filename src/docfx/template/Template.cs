// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class Template
    {
        private static readonly HashSet<string> s_excludedHtmlMetaTags = new HashSet<string>
        {
            "absolutePath",
            "canonical_url",
            "content_git_url",
            "open_to_public_contributors",
            "fileRelativePath",
            "layout",
            "title",
            "redirect_url",
            "contributors_to_exclude",
            "f1_keywords",
        };

        private static readonly string[] s_resourceFolders = new[] { "global", "css", "fonts" };

        private readonly string _templateDir;
        private readonly LiquidTemplate _liquid;
        private readonly MustacheTemplate _mustache;
        private readonly JavaScript _js;

        public Template(string templateDir)
        {
            var contentTemplateDir = Path.Combine(templateDir, "ContentTemplate");

            _templateDir = templateDir;
            _liquid = new LiquidTemplate(templateDir);
            _js = new JavaScript(contentTemplateDir);
            _mustache = new MustacheTemplate(contentTemplateDir);
        }

        public string Render(string schemaName, PageModel model)
        {
            // TODO: only works for conceptual
            var content = model.Content.ToString();
            var fileMetadata = JObject.FromObject(model.Metadata);

            var obj = JObject.FromObject(model);
            obj.Remove("content");
            obj.Remove("metadata");

            obj.Merge(fileMetadata, JsonUtility.MergeSettings);

            var page = _js.Run($"{schemaName}.mta.json.js", JsonUtility.Merge(fileMetadata, obj));
            var metadata = CreateHtmlMetaTags(page);
            var layout = page.Value<string>("layout");
            var liquidModel = new JObject { ["content"] = content, ["page"] = page, ["metadata"] = metadata };

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
                    Parallel.ForEach(Directory.EnumerateFiles(srcDir), file =>
                    {
                        var outputFilePath = Path.Combine(outputPath, "_themes", file.Substring(_templateDir.Length + 1));
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                        File.Copy(file, outputFilePath, overwrite: true);
                    });
                }
            }
        }

        public static string CreateHtmlMetaTags(JObject metadata)
        {
            var result = new StringBuilder();

            foreach (var (key, value) in metadata)
            {
                if (value is JObject || key.StartsWith("_op_") || s_excludedHtmlMetaTags.Contains(key))
                {
                    continue;
                }

                var content = "";
                if (value is JArray arr)
                {
                    if (!arr.All(item => item is JValue))
                    {
                        continue;
                    }

                    content = string.Join(",", value);
                }
                else if (value.Type == JTokenType.Boolean)
                {
                    content = (bool)value ? "true" : "false";
                }
                else
                {
                    content = value.ToString();
                }

                result.AppendLine($"<meta name=\"{HttpUtility.HtmlEncode(key)}\" content=\"{HttpUtility.HtmlEncode(content)}\" />");
            }

            return result.ToString();
        }
    }
}
