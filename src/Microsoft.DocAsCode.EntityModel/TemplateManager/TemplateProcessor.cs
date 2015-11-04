// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Jint;
    using Newtonsoft.Json;
    using Nustache.Core;

    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class TemplateProcessor : IDisposable
    {
        private static Regex IncludeRegex = new Regex(@"{{\s*!\s*include\s*\(:?(:?['""]?)\s*(?<file>(.+?))\1\s*\)\s*}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private const string ManifestFileName = ".manifest";
        private const string PartialTemplateExtension = ".tmpl.partial";
        private const string Language = "csharp"; // TODO: how to handle multi-language
        private TemplateCollection _templates;
        private ResourceCollection _resourceProvider = null;
        private ResourceTemplateLocator _resourceTemplateLocator;

        /// <summary>
        /// TemplateName can be either file or folder
        /// 1. If TemplateName is file, it is considered as the default template
        /// 2. If TemplateName is a folder, files inside the folder is considered as the template, each file is named after {DocumentType}.{extension}
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="resourceProvider"></param>
        public TemplateProcessor(ResourceCollection resourceProvider)
        {
            _resourceProvider = resourceProvider;
            _templates = new TemplateCollection(resourceProvider);
            _resourceTemplateLocator = new ResourceTemplateLocator(resourceProvider);
        }

        public void Process(DocumentBuildContext context, string outputDirectory)
        {
            var baseDirectory = context.BuildOutputFolder;

            if (string.IsNullOrEmpty(outputDirectory)) outputDirectory = Environment.CurrentDirectory;
            if (string.IsNullOrEmpty(baseDirectory)) baseDirectory = Environment.CurrentDirectory;

            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

            // 1. Copy dependent files with path relative to the base output directory
            ProcessDependencies(outputDirectory);
            Dictionary<string, HashSet<string>> unProcessedType = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> extMapping = new Dictionary<string, string>();
            // 2. Get extension for each item
            foreach (var item in context.Manifest)
            {
                if (item.ModelFile == null) throw new ArgumentNullException("Model file path must be specified!");
                if (item.DocumentType == null) throw new ArgumentNullException($"Document type is not allowed to be NULL for ${item.ModelFile}!");
                // NOTE: Resource is not supported for applying templates
                if (item.DocumentType.Equals("Resource", StringComparison.OrdinalIgnoreCase)) continue;
                var templates = _templates[item.DocumentType];
                // Get default template extension
                if (templates == null || templates.Count == 0)
                {
                    HashSet<string> unProcessedFiles;
                    if (unProcessedType.TryGetValue(item.DocumentType, out unProcessedFiles))
                    {
                        unProcessedFiles.Add(item.ModelFile);
                    }
                    else
                    {
                        unProcessedType[item.DocumentType] = new HashSet<string>(FilePathComparer.OSPlatformSensitiveComparer) { item.ModelFile };
                    }
                }
                else
                {
                    var defaultTemplate = templates.FirstOrDefault(s => s.IsPrimary) ?? templates[0];
                    string key = ((RelativePath)item.OriginalFile).GetPathFromWorkingFolder();
                    string value;
                    if (context.FileMap.TryGetValue(key, out value))
                    {
                        context.FileMap[key] = Path.ChangeExtension(value, defaultTemplate.Extension);
                        extMapping[key] = defaultTemplate.Extension;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Warning, $"{key} is not found in .filemap");
                    }
                }
            }

            //update internal XrefMap
            if (context.XRefSpecMap != null)
            {
                foreach (var pair in context.XRefSpecMap)
                {
                    string ext;
                    if (extMapping.TryGetValue(pair.Value.Href, out ext))
                    {
                        pair.Value.Href = Path.ChangeExtension(pair.Value.Href, ext);
                    }
                }
            }

            if (unProcessedType.Count > 0)
            {
                StringBuilder sb = new StringBuilder("There is no template processing:");
                foreach (var type in unProcessedType)
                {
                    sb.AppendLine($"- Document type: \"{type.Key}\"");
                    sb.AppendLine($"- Files:");
                    foreach (var file in type.Value)
                    {
                        sb.AppendLine($"  -\"{file}\"");
                    }
                }
                Logger.Log(LogLevel.Warning, sb.ToString());// not processed but copied to '{modelOutputPath}'");
            }

            List<TemplateManifestItem> manifest = new List<TemplateManifestItem>();

            // 3. Process every model and save to output directory
            foreach (var item in context.Manifest)
            {
                var manifestItem = new TemplateManifestItem
                {
                    DocumentType = item.DocumentType,
                    OriginalFile = item.LocalPathFromRepoRoot,
                    OutputFiles = new Dictionary<string, string>()
                };
                try
                {
                    var templates = _templates[item.DocumentType];
                    // 1. process model
                    if (templates == null)
                    {
                        // TODO: what if template to transform the type is not found? DO NOTHING?
                        // CopyFile(modelFile, modelOutputPath);
                    }
                    else
                    {
                        var modelFile = Path.Combine(baseDirectory, item.ModelFile);
                        var systemAttrs = new SystemAttributes(context, item);
                        foreach (var template in templates)
                        {
                            var extension = template.Extension;
                            string outputFile = Path.ChangeExtension(item.ModelFile, extension);
                            string outputPath = Path.Combine(outputDirectory ?? string.Empty, outputFile);
                            var dir = Path.GetDirectoryName(outputPath);
                            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                            var transformed = template.Transform(modelFile, systemAttrs, _resourceTemplateLocator.GetTemplate);
                            if (!string.IsNullOrWhiteSpace(transformed))
                            {
                                if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
                                {
                                    TranformHtml(context, transformed, item.ModelFile, outputPath);
                                }
                                else
                                {
                                    File.WriteAllText(outputPath, transformed, Encoding.UTF8);
                                }

                                Logger.Log(LogLevel.Verbose, $"Transformed model \"{item.ModelFile}\" to \"{outputPath}\".");
                            }
                            else
                            {
                                // TODO: WHAT to do if is transformed to empty string? STILL creat empty file?
                                Logger.Log(LogLevel.Warning, $"Model \"{item.ModelFile}\" is transformed to empty string with template \"{template.Name}\"");
                                File.WriteAllText(outputPath, string.Empty);
                            }
                            manifestItem.OutputFiles.Add(extension, outputFile);
                        }
                    }

                    // 2. process resource
                    if (item.ResourceFile != null)
                    {
                        manifestItem.OutputFiles.Add("resource", item.ResourceFile);
                        PathUtility.CopyFile(Path.Combine(baseDirectory, item.ResourceFile), Path.Combine(outputDirectory, item.ResourceFile), true);
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, $"Unable to transform {item.ModelFile}: {e.Message}. Ignored.");
                }
                manifest.Add(manifestItem);
            }

            // Save manifest
            var manifestPath = Path.Combine(outputDirectory, ManifestFileName);
            JsonUtility.Serialize(manifestPath, manifest);
            Logger.Log(LogLevel.Verbose, $"Manifest file saved to {manifestPath}.");
        }

        private void TranformHtml(DocumentBuildContext context, string transformed, string relativeModelPath, string outputPath)
        {
            // Update HREF and XREF
            HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
            html.LoadHtml(transformed);
            var srcNodes = html.DocumentNode.SelectNodes("//*/@src");
            if (srcNodes != null)
                foreach (var link in srcNodes)
                {
                    UpdateSrc(link, context.FileMap, s => UpdateFilePath(s, relativeModelPath));
                }

            var hrefNodes = html.DocumentNode.SelectNodes("//*/@href");
            if (hrefNodes != null)
                foreach (var link in hrefNodes)
                {
                    // xref is generated by docfx, and is lower-cased
                    if (link.Name == "xref")
                    {
                        UpdateXref(link, context.XRefSpecMap, context.ExternalXRefSpec, s => UpdateFilePath(s, relativeModelPath), Language);
                    }
                    else
                    {
                        UpdateHref(link, context.FileMap, s => UpdateFilePath(s, relativeModelPath));
                    }
                }

            // Save with extension changed
            var subDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(subDirectory) && !Directory.Exists(subDirectory)) Directory.CreateDirectory(subDirectory);
            html.Save(outputPath, Encoding.UTF8);
        }

        private void ProcessDependencies(string outputDirectory)
        {
            if (_resourceProvider == null)
            {
                Logger.Log(LogLevel.Info, "Resource provider is not specified, dependencies will not be processed.");
                return;
            }

            if (_templates != null)
            {
                foreach (var resourceInfo in ExtractDependentFilePaths(_templates).Distinct())
                {
                    var filePath = resourceInfo.FilePath;
                    try
                    {
                        var stream = _resourceProvider.GetResourceStream(resourceInfo.ResourceKey);
                        if (stream != null)
                        {
                            var path = Path.Combine(outputDirectory, filePath);
                            var dir = Path.GetDirectoryName(path);
                            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                            using (stream)
                            {
                                using (var writer = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                                {
                                    stream.CopyTo(writer);
                                }
                            }

                            Logger.Log(LogLevel.Verbose, $"Saved resource {filePath} that template dependants on to {path}");
                        }
                        else
                        {
                            Logger.Log(LogLevel.Info, $"Unable to get relative resource for {filePath}");
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogLevel.Info, $"Unable to get relative resource for {filePath}: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Dependent files are defined in following syntax in Mustache template leveraging Mustache Comments
        /// {{! include('file') }}
        /// file path can be wrapped by quote ' or double quote " or none
        /// </summary>
        /// <param name="template"></param>
        private IEnumerable<TemplateResourceInfo> ExtractDependentFilePaths(TemplateCollection templates)
        {
            foreach (var templateList in templates.Values)
            {
                foreach (var template in templateList)
                {
                    foreach (Match match in IncludeRegex.Matches(template.Content))
                    {
                        var filePath = match.Groups["file"].Value;
                        if (string.IsNullOrWhiteSpace(filePath)) yield break;
                        if (filePath.StartsWith("./")) filePath = filePath.Substring(2);
                        yield return new TemplateResourceInfo(template.GetRelativeResourceKey(filePath), filePath);
                    }
                }
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

        private static void UpdateXref(HtmlAgilityPack.HtmlNode xref, Dictionary<string, XRefSpec> internalXRefMap, Dictionary<string, XRefSpec> externalXRefMap, Func<string, string> updater, string language)
        {
            string attribute = "href";
            var key = xref.GetAttributeValue(attribute, null);
            var name = xref.GetAttributeValue("name", null);
            var displayName = name ?? key;
            XRefSpec spec = null;
            if (internalXRefMap.TryGetValue(key, out spec))
            {
                xref.Name = "a";
                spec.Href = updater(spec.Href);
                displayName = GetLanguageSpecificAttribute(spec, language, displayName, "name");

                xref.SetAttributeValue(attribute, spec.Href);
                xref.AppendChild(HtmlAgilityPack.HtmlNode.CreateNode(displayName));
                return;
            }

            if (externalXRefMap.TryGetValue(key, out spec))
            {
                if (!string.IsNullOrEmpty(spec.Href))
                {
                    xref.Name = "a";
                    displayName = GetLanguageSpecificAttribute(spec, language, displayName, "name");
                    xref.SetAttributeValue(attribute, spec.Href);
                    xref.AppendChild(HtmlAgilityPack.HtmlNode.CreateNode(displayName));
                    return;
                }
            }

            if (spec != null)
            {
                displayName = GetLanguageSpecificAttribute(spec, language, displayName, "fullName", "name");
            }

            xref.ParentNode.ReplaceChild(HtmlAgilityPack.HtmlNode.CreateNode(displayName), xref);
        }

        private static string GetLanguageSpecificAttribute(XRefSpec spec, string language, string defaultValue, params string[] keyInFallbackOrder)
        {
            if (keyInFallbackOrder == null || keyInFallbackOrder.Length == 0) throw new ArgumentException("key must be provided!", nameof(keyInFallbackOrder));
            string suffix = string.Empty;
            if (!string.IsNullOrEmpty(language)) suffix = "." + language;
            foreach(var key in keyInFallbackOrder)
            {
                string value;
                var keyWithSuffix = key + suffix;
                if (spec.TryGetValue(keyWithSuffix, out value))
                {
                    return value;
                }
                if (spec.TryGetValue(key, out value))
                {
                    return value;
                }
            }

            return defaultValue;
        }

        private static void UpdateHref(HtmlAgilityPack.HtmlNode link, Dictionary<string, string> map, Func<string, string> updater)
        {
            string attribute = "href";
            var key = link.GetAttributeValue(attribute, null);
            string path;
            if (TryGetPathToRoot(key, out path))
            {
                string href;
                // For href, # may be appended, remove # before search file from map
                var anchorIndex = key.IndexOf("#");
                var anchor = string.Empty;
                if (anchorIndex == 0) return;
                if (anchorIndex > 0)
                {
                    anchor = key.Substring(anchorIndex);
                    key = key.Remove(anchorIndex);
                }

                if (map.TryGetValue(key, out href))
                {
                    href = updater(href);
                    href += anchor;
                    link.SetAttributeValue(attribute, href);
                }
                else
                {
                    Logger.Log(LogLevel.Warning, $"File {path} is not found.");
                    // TODO: what to do if file path not exists?
                    // CURRENT: fallback to the original one
                    link.SetAttributeValue(attribute, path);
                }
            }
        }

        private static void UpdateSrc(HtmlAgilityPack.HtmlNode link, Dictionary<string, string> map, Func<string, string> updater)
        {
            string attribute = "src";
            var key = link.GetAttributeValue(attribute, null);
            string path;
            if (TryGetPathToRoot(key, out path))
            {
                string xrefValue;
                if (map.TryGetValue(key, out xrefValue))
                {
                    xrefValue = updater(xrefValue);
                    link.SetAttributeValue(attribute, xrefValue);
                }
                else
                {
                    Logger.Log(LogLevel.Warning, $"File {path} is not found.");
                    // TODO: what to do if file path not exists?
                    // CURRENT: fallback to the original one
                    link.SetAttributeValue(attribute, path);
                }
            }
        }

        private static string UpdateFilePath(string path, string modelFilePathToRoot)
        {
            string pathToRoot;
            if (TryGetPathToRoot(path, out pathToRoot))
            {
                return ((RelativePath)pathToRoot).MakeRelativeTo((RelativePath)modelFilePathToRoot);
            }
            return path;
        }

        private static bool TryGetPathToRoot(string path, out string pathToRoot)
        {
            if (!string.IsNullOrEmpty(path) && IsMappedPath(path))
            {
                pathToRoot = path.Substring(2);
                return true;
            }
            pathToRoot = path;
            return false;
        }

        private static bool IsMappedPath(string path)
        {
            return ((RelativePath)path).IsFromWorkingFolder();
        }

        public void Dispose()
        {
            _resourceProvider?.Dispose();
        }

        private sealed class SystemAttributes
        {
            [JsonProperty("_lang")]
            public string Language { get; set; }
            [JsonProperty("_title")]
            public string Title { get; set; }
            [JsonProperty("_tocTitle")]
            public string TocTitle { get; set; }
            [JsonProperty("_name")]
            public string Name { get; set; }
            [JsonProperty("_description")]
            public string Description { get; set; }

            /// <summary>
            /// TOC PATH from ~ ROOT
            /// </summary>
            [JsonProperty("_tocPath")]
            public string TocPath { get; set; }

            /// <summary>
            /// ROOT TOC PATH from ~ ROOT
            /// </summary>
            [JsonProperty("_navPath")]
            public string RootTocPath { get; set; }

            /// <summary>
            /// Current file's relative path to ROOT, e.g. file is ~/A/B.md, relative path to ROOT is ../
            /// </summary>
            [JsonProperty("_rel")]
            public string RelativePathToRoot { get; set; }

            /// <summary>
            /// ROOT TOC file's relative path to ROOT
            /// </summary>
            [JsonProperty("_navRel")]
            public string RootTocRelativePath { get; set; }

            /// <summary>
            /// current file's TOC file's relative path to ROOT
            /// </summary>
            [JsonProperty("_tocRel")]
            public string TocRelativePath { get; set; }

            public SystemAttributes(DocumentBuildContext context, ManifestItem item)
            {
                Language = TemplateProcessor.Language;
                GetTocInfo(context, item);
                TocRelativePath = TocPath == null ? null : ((RelativePath)TocPath).MakeRelativeTo((RelativePath)item.ModelFile);
                RootTocRelativePath = RootTocPath == null ? null : ((RelativePath)RootTocPath).MakeRelativeTo((RelativePath)item.ModelFile);
                RelativePathToRoot = (RelativePath.Empty).MakeRelativeTo((RelativePath)item.ModelFile);
            }

            private void GetTocInfo(DocumentBuildContext context, ManifestItem item)
            {
                string relativePath = item.OriginalFile;
                var tocMap = context.TocMap;
                var fileMap = context.FileMap;
                HashSet<string> parentTocs;
                string parentToc = null;
                string rootToc = null;
                string currentPath = ((RelativePath)relativePath).GetPathFromWorkingFolder();
                while (tocMap.TryGetValue(currentPath, out parentTocs) && parentTocs.Count > 0)
                {
                    // Get the first toc only
                    currentPath = parentTocs.First();
                    rootToc = currentPath;
                    if (parentToc == null) parentToc = currentPath;
                    currentPath = ((RelativePath)currentPath).GetPathFromWorkingFolder();
                }
                if (rootToc != null)
                {
                    rootToc = fileMap[((RelativePath)rootToc).GetPathFromWorkingFolder()];
                    TryGetPathToRoot(rootToc, out rootToc);
                    RootTocPath = rootToc;
                }

                if (parentToc == null) TocPath = RootTocPath;
                else
                {
                    parentToc = fileMap[((RelativePath)parentToc).GetPathFromWorkingFolder()];
                    TryGetPathToRoot(parentToc, out parentToc);
                    TocPath = parentToc;
                }
            }
        }

        private sealed class ResourceTemplateLocator
        {
            private ResourceCollection _resourceProvider;
            public ResourceTemplateLocator(ResourceCollection resourceProvider)
            {
                _resourceProvider = resourceProvider;
            }

            public Nustache.Core.Template GetTemplate(string name)
            {
                if (_resourceProvider == null) return null;
                var resourceName = name + PartialTemplateExtension;
                using (var stream = _resourceProvider.GetResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    var template = new Nustache.Core.Template(name);
                    using (StreamReader reader = new StreamReader(stream))
                        template.Load(reader);
                    return template;
                }
            }
        }

        private class Template
        {
            private string _script = null;

            public string Content { get; }
            public string Name { get; }
            public string Extension { get; }
            public string Type { get; }
            public bool IsPrimary { get; }
            public Template(string template, string templateName, string script)
            {
                Name = templateName;
                Content = template;
                var typeAndExtension = GetTemplateTypeAndExtension(templateName);
                Extension = typeAndExtension.Item2;
                Type = typeAndExtension.Item1;
                IsPrimary = typeAndExtension.Item3;
                _script = script;
            }

            public string GetRelativeResourceKey(string relativePath)
            {
                return Path.Combine(Path.GetDirectoryName(this.Name ?? string.Empty) ?? string.Empty, relativePath).ToNormalizedPath();
            }

            public string Transform(string modelPath, object attrs, TemplateLocator templateLocator)
            {
                if (_script == null)
                {
                    var entity = JsonUtility.Deserialize<object>(modelPath);
                    return Render.StringToString(Content, entity, templateLocator);
                }
                else
                {

                    var processedModel = ProcessWithJint(File.ReadAllText(modelPath), attrs);
                    return Render.StringToString(Content, processedModel, templateLocator);
                }
            }

            private object ProcessWithJint(string model, object attrs)
            {
                var engine = new Engine();

                // engine.SetValue("model", stream.ToString());
                engine.SetValue("console", new
                {
                    log = new Action<object>(Logger.Log)
                });

                // throw exception when execution fails
                engine.Execute(_script);
                var value = engine.Invoke("transform", model, JsonUtility.Serialize(attrs)).ToObject();

                // var value = engine.GetValue("model").ToObject();
                // The results generated
                return value;
            }

            private static Tuple<string, string, bool> GetTemplateTypeAndExtension(string templateName)
            {
                // Remove folder and .tmpl
                templateName = Path.GetFileNameWithoutExtension(templateName);
                var splitterIndex = templateName.IndexOf('.');
                if (splitterIndex < 0) return Tuple.Create(templateName, string.Empty, false);
                var type = templateName.Substring(0, splitterIndex);
                var extension = templateName.Substring(splitterIndex);
                var isPrimary = false;
                if (extension.EndsWith(".primary"))
                {
                    isPrimary = true;
                    extension = extension.Substring(0, extension.Length - 8);
                }
                return Tuple.Create(type, extension, isPrimary);
            }
        }
        private class TemplateCollection : Dictionary<string, List<Template>>
        {
            private List<Template> _defaultTemplate = null;

            public new List<Template> this[string key]
            {
                get
                {
                    List<Template> template;
                    if (key != null && this.TryGetValue(key, out template))
                    {
                        return template;
                    }

                    return _defaultTemplate;
                }
                set
                {
                    this[key] = value;
                }
            }

            public TemplateCollection(ResourceCollection provider) : base(ReadTemplate(provider), StringComparer.OrdinalIgnoreCase)
            {
                base.TryGetValue("default", out _defaultTemplate);
            }

            private static Dictionary<string, List<Template>> ReadTemplate(ResourceCollection resource)
            {
                // type <=> list of template with different extension
                var dict = new Dictionary<string, List<Template>>(StringComparer.OrdinalIgnoreCase);
                if (resource == null) return dict;
                // Template file ends with .tmpl
                // Template file naming convention: {template file name}.{file extension}.tmpl
                var templates = resource.GetResources(@".*\.(tmpl|js)$").ToList();
                if (templates != null)
                {
                    foreach (var group in templates.GroupBy(s => Path.GetFileNameWithoutExtension(s.Key), StringComparer.OrdinalIgnoreCase))
                    {
                        var currentTemplates = group.Where(s => Path.GetExtension(s.Key).Equals(".tmpl", StringComparison.OrdinalIgnoreCase)).ToArray();
                        var currentScripts = group.Where(s => Path.GetExtension(s.Key).Equals(".js", StringComparison.OrdinalIgnoreCase)).ToArray();
                        var currentTemplate = currentTemplates.FirstOrDefault();
                        var currentScript = currentScripts.FirstOrDefault();
                        if (currentTemplates.Length > 1)
                        {
                            Logger.Log(LogLevel.Warning, $"Multiple templates for type '{group.Key}'(case insensitive) are found, the one from '{currentTemplates[0].Key}' is taken.");
                        }
                        else if (currentTemplates.Length == 0)
                        {
                            // If template does not exist, ignore
                            continue;
                        }

                        if (currentScripts.Length > 1)
                        {
                            Logger.Log(LogLevel.Warning, $"Multiple template scripts for type '{group.Key}'(case insensitive) are found, the one from '{currentScripts[0].Key}' is taken.");
                        }

                        var template = new Template(currentTemplate.Value, currentTemplate.Key, currentScript.Value);
                        List<Template> templateList;
                        if (dict.TryGetValue(template.Type, out templateList))
                        {
                            templateList.Add(template);
                        }
                        else
                        {
                            dict[template.Type] = new List<Template> { template };
                        }
                    }
                }

                return dict;
            }
        }
    }
}
