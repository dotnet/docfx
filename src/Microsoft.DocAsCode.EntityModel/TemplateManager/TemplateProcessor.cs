// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using Nustache.Core;
    using Utility;
    using Jint;
    using System.Linq;
    using Builders;
    using System.Collections;

    public class TemplateProcessor : IDisposable
    {
        private static Regex IncludeRegex = new Regex(@"{{\s*!\s*include\s*\(:?(:?['""]?)\s*(?<file>(.+?))\1\s*\)\s*}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private string _script = null;

        private TemplateCollection _templates;
        private ResourceCollection _resourceProvider = null;

        /// <summary>
        /// TemplateName can be either file or folder
        /// 1. If TemplateName is file, it is considered as the default template
        /// 2. If TemplateName is a folder, files inside the folder is considered as the template, each file is named after {DocumentType}.{extension}
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="resourceProvider"></param>
        public TemplateProcessor(string templateName, ResourceCollection resourceProvider)
        {
            _resourceProvider = resourceProvider;
            _templates = new TemplateCollection(templateName, resourceProvider);
            var scriptName = templateName + ".js";
            _script = resourceProvider?.GetResource(scriptName);
            ParseResult.WriteToConsole(ResultLevel.Info, $"Using template {templateName}" + _script == null ? string.Empty : $" and pre-process script {scriptName}");
        }

        public void Process(DocumentBuildContext context, string outputDirectory)
        {
            var baseDirectory = context.BuildOutputFolder;

            // href/src file id mapping: string<->string from which file to which file
            var fileMap = context.FileMap;

            // xref id mapping: string<->string from which xref to which xref
            var xref = context.XRefMap;

            // model file: convert; resource file: copy; type: decide which template to apply
            var manifest = context.Manifest;

            if (string.IsNullOrEmpty(outputDirectory)) outputDirectory = Environment.CurrentDirectory;
            if (string.IsNullOrEmpty(baseDirectory)) baseDirectory = Environment.CurrentDirectory;

            bool isSameFolder = false;
            if (FilePathComparer.OSPlatformSensitiveComparer.Equals(outputDirectory, baseDirectory)) isSameFolder = true;

            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

            // 1. Copy dependent files with path relative to the base output directory
            ProcessDependencies(outputDirectory);
            if (_template == null)
            {
                ParseResult.WriteToConsole(ResultLevel.Info, "No template will be applied");
                return;
            }
            
            // 2. Process every model and save to output directory
            foreach (var item in manifest)
            {
                try
                {
                    object model;
                    using (var reader = new StreamReader(Path.Combine(baseDirectory, item.ModelFile)))
                    {
                        model = YamlUtility.Deserialize<object>(reader);
                        var template = _templates[item.DocumentType];
                        // 1. process model
                        if (template == null)
                        {
                            ParseResult.WriteToConsole(ResultLevel.Warning, $"Unable to find template for '{item.DocumentType}', '{item.ModelFile}' is not processed.");
                        }
                        else
                        {
                            var transformed = Transform(model, template);
                            var extension = template.Extension;
                            if (!string.IsNullOrWhiteSpace(transformed))
                            {
                                // Update HREF and XREF
                                HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
                                html.LoadHtml(transformed);
                                var srcNodes = html.DocumentNode.SelectNodes("//*/@src");
                                if (srcNodes != null)
                                    foreach (var link in srcNodes)
                                    {
                                        UpdateHref(link, "src", fileMap, s => Path.ChangeExtension(s, extension));
                                    }

                                var hrefNodes = html.DocumentNode.SelectNodes("//*/@href");
                                if (hrefNodes != null)
                                    foreach (var link in hrefNodes)
                                    {
                                        // xref is generated by docfx, and is lower-cased
                                        if (link.Name == "xref")
                                        {
                                            UpdateXref(link, xref, s => Path.ChangeExtension(s, extension));
                                        }
                                        else
                                        {
                                            UpdateHref(link, "href", fileMap, s => Path.ChangeExtension(s, extension));
                                        }
                                    }

                                // Save with extension changed
                                var modelOutputPath = Path.Combine(outputDirectory, Path.ChangeExtension(item.ModelFile, extension));
                                var subDirectory = Path.GetDirectoryName(modelOutputPath);
                                if (!string.IsNullOrEmpty(subDirectory) && !Directory.Exists(subDirectory)) Directory.CreateDirectory(subDirectory);
                                File.WriteAllText(modelOutputPath, transformed);
                                ParseResult.WriteToConsole(ResultLevel.Success, "Transformed model {0} to {1}.", item.ModelFile, modelOutputPath);
                            }
                            else
                            {
                                ParseResult.WriteToConsole(ResultLevel.Info, "Model {0} is transformed to empty string, ignored.", item.ModelFile);
                            }
                        }

                        // 2. process resource
                        if (!isSameFolder && item.ResourceFile != null)
                        {
                            File.Copy(Path.Combine(baseDirectory, item.ResourceFile), Path.Combine(outputDirectory, item.ResourceFile));
                        }
                    }
                }
                catch (Exception e)
                {
                    ParseResult.WriteToConsole(ResultLevel.Warning, $"Unable to transform {item.ModelFile}: {e.Message}. Ignored.");
                }
            }
        }

        private void ProcessDependencies(string outputDirectory)
        {
            if (_resourceProvider == null)
            {
                ParseResult.WriteToConsole(ResultLevel.Info, "Resource provider is not specified, dependencies will not be processed.");
                return;
            }

            if (_templates != null)
            {
                foreach (var filePath in ExtractDependentFilePaths(_templates).Distinct())
                {
                    try
                    {
                        var stream = _resourceProvider.GetResourceStream(resourceInfo.ResourceKey);
                        if (stream != null)
                        {
                            var path = Path.Combine(outputDirectory, resourceInfo.FilePath);
                            var dir = Path.GetDirectoryName(path);
                            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                            using (stream)
                            {
                                using(var writer = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                                {
                                    stream.CopyTo(writer);
                                }
                            }

                            ParseResult.WriteToConsole(ResultLevel.Info, "Saved resource {0} that template dependants on to {1}", resourceInfo.ResourceKey, path);
                        }
                        else
                        {
                            ParseResult.WriteToConsole(ResultLevel.Info, "Unable to get relative resource for {0}", resourceInfo.ResourceKey);
                        }
                    }
                    catch (Exception e)
                    {
                        ParseResult.WriteToConsole(ResultLevel.Info, "Unable to get relative resource for {0}: {1}", resourceInfo.ResourceKey, e.Message);
                    }
                }
            }
        }

        /// <summary>
        /// 1. Find Template zip file or folder with provided name
        /// 2. If {name}.js file exists, run it
        /// </summary>
        /// <param name="model">The model to be parsed</param>
        private string Transform(object model, Template template)
        {
            if (_script == null)
            {
                return Render.StringToString(template.Content, model);
            }
            else
            {
                var processedModel = ProcessWithJint(model);
                return Render.StringToString(template.Content, processedModel);
            }
        }

        /// <summary>
        /// Dependent files are defined in following syntax in Mustache template leveraging Mustache Comments
        /// {{! include('file') }}
        /// file path can be wrapped by quote ' or double quote " or none
        /// </summary>
        /// <param name="template"></param>
        private IEnumerable<string> ExtractDependentFilePaths(TemplateCollection templates)
        {
            foreach(var template in templates)
            {
                foreach (Match match in IncludeRegex.Matches(template.Value.Content))
                {
                    var filePath = match.Groups["file"].Value;
                    if (string.IsNullOrWhiteSpace(filePath)) yield break;
                    if (filePath.StartsWith("./")) filePath = filePath.Substring(2);
                    yield return template.Value.GetRelativeResourceKey(filePath);
                }
            }
        }

        private class Template
        {
            public string Content { get; }
            public string Name { get; }
            public string Type { get; }
            public Template(string template, string templateName)
            {
                Name = templateName;
                Content = template;
            }

            public string GetRelativeResourceKey(string relativePath)
            {
                return Path.Combine(Path.GetDirectoryName(this.Name ?? string.Empty) ?? string.Empty, relativePath).ToNormalizedPath();
            }
        }

        private sealed class TemplateResourceInfo
        {
            public string ResourceKey { get; }
            public string FilePath { get; }
            public TemplateResourceInfo(string resourceKey, string filePath)
            {
                ResourceKey = resourceKey;
                FilePath = filePath;
            }
        }
        private object ProcessWithJint(object model)
        {
            using (var stream = new StringWriter())
            {
                JsonUtility.Serialize(stream, model);

                var engine = new Engine();
                // engine.SetValue("model", stream.ToString());
                engine.SetValue("console", new
                {
                    log = new Action<object>(ParseResult.WriteInfo)
                });

                // throw exception when execution fails
                engine.Execute(_script);
                var value = engine.Invoke("transform", stream.ToString()).ToObject();

                // var value = engine.GetValue("model").ToObject();
                // The results generated
                return value;
            }
        }

        private static void UpdateXref(HtmlAgilityPack.HtmlNode xref, Dictionary<string, string> map, Func<string, string> updater)
        {
            xref.Name = "a";
            var key = xref.GetAttributeValue("href", null);

            if (IsMappedPath(key))
            {
                string xrefValue;
                if (map.TryGetValue(key, out xrefValue))
                {
                    xrefValue = updater(xrefValue);
                    xref.AppendChild(HtmlAgilityPack.HtmlNode.CreateNode(key));
                }
            }
        }

        private static void UpdateHref(HtmlAgilityPack.HtmlNode link, string attribute, Dictionary<string, string> map, Func<string, string> updater)
        {
            var key = link.GetAttributeValue(attribute, null);
            if (IsMappedPath(key))
            {
                string xrefValue;
                if (map.TryGetValue(key, out xrefValue))
                {
                    xrefValue = updater(xrefValue);
                    link.SetAttributeValue(attribute, xrefValue);
                }
            }
        }

        private static bool IsMappedPath(string path)
        {
            return path.StartsWith("~/");
        }

        public void Dispose()
        {
            _resourceProvider?.Dispose();
        }

        private class Template
        {
            public string Content { get; }
            public string Name { get; }
            public string Extension { get; }
            public string Type { get; }
            public Template(string template, string templateName)
            {
                Name = templateName;
                Content = template;
                Extension = GetFileExtensionFromTemplate(templateName);
                Type = GetTemplateTypeFromTemplateName(templateName);
            }

            public string GetRelativeResourceKey(string relativePath)
            {
                return Path.Combine(Path.GetDirectoryName(this.Name ?? string.Empty) ?? string.Empty, relativePath).ToNormalizedPath();
            }

            private static string GetFileExtensionFromTemplate(string templateName)
            {
                return Path.GetExtension(Path.GetFileNameWithoutExtension(templateName ?? string.Empty));
            }

            private static string GetTemplateTypeFromTemplateName(string templateName)
            {
                return Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(templateName));
            }
        }
        private class TemplateCollection : Dictionary<string, Template>
        {
            private ResourceCollection _resourceProvider;
            private string _templateName;
            public Template DefaultTemplate { get; private set; } = null;

            public new Template this[string key]
            {
                get
                {
                    Template template;
                    if (key != null && this.TryGetValue(key, out template))
                    {
                        return template;
                    }

                    return DefaultTemplate;
                }
                set
                {
                    this[key] = value;
                }
            }

            public TemplateCollection(string templateName, ResourceCollection provider) : base(ReadTemplate(templateName, provider))
            {
                if (string.IsNullOrEmpty(templateName)) throw new ArgumentNullException(nameof(templateName));
                _resourceProvider = provider;
                _templateName = templateName;
                var defaultTemplateResource = provider?.GetResource($"{templateName}.tmpl");
                if (defaultTemplateResource != null)
                    DefaultTemplate = new Template(defaultTemplateResource, $"{templateName}.tmpl");
            }

            private static Dictionary<string, Template> ReadTemplate(string templateName, ResourceCollection resource)
            {
                var dict = new Dictionary<string, Template>();
                if (resource == null) return dict;
                // Template file ends with .tmpl
                // Template file naming convention: {template file name}.{file extension}.tmpl
                var templates = resource.GetResources($"**.tmpl");
                if (templates != null)
                {
                    foreach (var item in templates)
                    {
                        var template = new Template(item.Value, item.Key);
                        Template saved;
                        if (dict.TryGetValue(template.Type, out saved))
                        {
                            ParseResult.WriteToConsole(ResultLevel.Warning, $"Multiple template for type '{saved.Type}' is found, The one from '{saved.Name}' is taken.");
                        }
                        else
                        {
                            dict[template.Type] = template;
                        }
                    }
                }

                return dict;
            }
        }
    }
}
