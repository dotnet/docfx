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

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class TemplateProcessor : IDisposable
    {
        private const string ManifestFileName = ".manifest";
        private const string Language = "csharp"; // TODO: how to handle multi-language
        private ResourceCollection _resourceProvider = null;

        public TemplateCollection Templates { get; }

        public bool IsEmpty { get { return Templates.Count == 0; } }

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
            Templates = new TemplateCollection(resourceProvider);
        }

        // TODO: remove
        public void Process(DocumentBuildContext context, string outputDirectory)
        {
            var baseDirectory = context.BuildOutputFolder;

            if (string.IsNullOrEmpty(outputDirectory)) outputDirectory = Environment.CurrentDirectory;
            if (string.IsNullOrEmpty(baseDirectory)) baseDirectory = Environment.CurrentDirectory;

            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

            // 1. Copy dependent files with path relative to the base output directory
            ProcessDependencies(outputDirectory);
            UpdateFileMap(context, outputDirectory, Templates);
            List<TemplateManifestItem> manifest = new List<TemplateManifestItem>();

            // 3. Process every model and save to output directory
            foreach (var item in context.Manifest)
            {
                var manifestItem = Transform(context, item, Templates, outputDirectory, true, s => s + ".json");
                manifest.Add(manifestItem);
            }

            // Save manifest
            var manifestPath = Path.Combine(outputDirectory, ManifestFileName);
            JsonUtility.Serialize(manifestPath, manifest);
            Logger.Log(LogLevel.Verbose, $"Manifest file saved to {manifestPath}.");
        }

        public static string UpdateFilePath(string path, string documentType, TemplateCollection templateCollection)
        {
            if (templateCollection == null) return path;
            var templates = templateCollection[documentType];

            // Get default template extension
            if (templates == null || templates.Count == 0) return path;

            var defaultTemplate = templates.FirstOrDefault(s => s.IsPrimary) ?? templates[0];
            return Path.ChangeExtension(path, defaultTemplate.Extension);
        }

        public static void UpdateFileMap(DocumentBuildContext context, string outputDirectory, TemplateCollection templateCollection)
        {
            //update internal XrefMap
            if (context.XRefSpecMap != null)
            {
                foreach (var pair in context.XRefSpecMap)
                {
                    string targetFilePath;
                    if (context.FileMap.TryGetValue(pair.Value.Href, out targetFilePath))
                    {
                        pair.Value.Href = targetFilePath;
                    }
                    else
                    {
                        Logger.LogWarning($"{pair.Value.Href} is not found in .filemap");
                    }
                }
            }

            context.SetExternalXRefSpec();
        }

        public static TemplateManifestItem Transform(DocumentBuildContext context, ManifestItem item, TemplateCollection templateCollection, string outputDirectory, bool exportMetadata, Func<string, string> metadataFilePathProvider)
        {
            var baseDirectory = context.BuildOutputFolder ?? string.Empty;
            var manifestItem = new TemplateManifestItem
            {
                DocumentType = item.DocumentType,
                OriginalFile = item.LocalPathFromRepoRoot,
                OutputFiles = new Dictionary<string, string>()
            };
            if (templateCollection == null || templateCollection.Count == 0)
            {
                return manifestItem;
            }
            try
            {
                var model = item.Model?.Content;
                var templates = templateCollection[item.DocumentType];
                // 1. process model
                if (templates == null)
                {
                    // Logger.LogWarning($"There is no template processing {item.DocumentType} document \"{item.LocalPathFromRepoRoot}\"");
                }
                else
                {
                    var modelFile = Path.Combine(baseDirectory, item.ModelFile);
                    var systemAttrs = new SystemAttributes(context, item, TemplateProcessor.Language);
                    foreach (var template in templates)
                    {
                        var extension = template.Extension;
                        string outputFile = Path.ChangeExtension(item.ModelFile, extension);
                        string outputPath = Path.Combine(outputDirectory ?? string.Empty, outputFile);
                        var dir = Path.GetDirectoryName(outputPath);
                        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                        string transformed;
                        if (model == null)
                        {
                            // TODO: remove
                            // currently keep to pass UT
                            transformed = template.Transform(item.ModelFile, systemAttrs);
                        }
                        else
                        {
                            var result = template.TransformModel(model, systemAttrs);

                            if (exportMetadata)
                            {
                                if (metadataFilePathProvider == null)
                                {
                                    throw new ArgumentNullException(nameof(metadataFilePathProvider));
                                }

                                JsonUtility.Serialize(metadataFilePathProvider(outputPath), result.Model);
                            }

                            transformed = result.Result;
                        }

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
                            Logger.LogWarning($"Model \"{item.ModelFile}\" is transformed to empty string with template \"{template.Name}\"");
                            File.WriteAllText(outputPath, string.Empty);
                        }
                        manifestItem.OutputFiles.Add(extension, outputFile);
                    }
                }

                // 2. process resource
                if (item.ResourceFile != null)
                {
                    PathUtility.CopyFile(Path.Combine(baseDirectory, item.ResourceFile), Path.Combine(outputDirectory, item.ResourceFile), true);
                    manifestItem.OutputFiles.Add("resource", item.ResourceFile);
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Unable to transform {item.ModelFile}: {e.Message}. Ignored.");
            }

            return manifestItem;
        }

        private static void TranformHtml(DocumentBuildContext context, string transformed, string relativeModelPath, string outputPath)
        {
            // Update HREF and XREF
            var internalXref = context.XRefSpecMap;
            var externalXref = context.ExternalXRefSpec;
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
                        UpdateXref(link, internalXref, externalXref, s => UpdateFilePath(s, relativeModelPath), Language);
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

        public void ProcessDependencies(string outputDirectory)
        {
            if (!IsEmpty)
            {
                foreach (var resourceInfo in ExtractDependentFilePaths(Templates).Distinct())
                {
                    try
                    {
                        // TODO: support glob pattern
                        if (resourceInfo.IsRegexPattern)
                        {
                            var regex = new Regex(resourceInfo.ResourceKey, RegexOptions.IgnoreCase);
                            foreach (var name in _resourceProvider.Names)
                            {
                                if (regex.IsMatch(name))
                                {
                                    using (var stream = _resourceProvider.GetResourceStream(name))
                                    {
                                        ProcessSingleDependency(stream, outputDirectory, name);
                                    }
                                }
                            }
                        }
                        else
                        {
                            using (var stream = _resourceProvider.GetResourceStream(resourceInfo.ResourceKey))
                            {
                                ProcessSingleDependency(stream, outputDirectory, resourceInfo.FilePath);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogLevel.Info, $"Unable to get relative resource for {resourceInfo.FilePath}: {e.Message}");
                    }
                }
            }
        }

        private IEnumerable<TemplateResourceInfo> ExtractDependentFilePaths(TemplateCollection templates)
        {
            return templates.Values.SelectMany(s => s).SelectMany(s => s.Resources);
        }

        private void ProcessSingleDependency(Stream stream, string outputDirectory, string filePath)
        {
            if (stream != null)
            {
                var path = Path.Combine(outputDirectory, filePath);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                using (var writer = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                {
                    stream.CopyTo(writer);
                }

                Logger.Log(LogLevel.Verbose, $"Saved resource {filePath} that template dependants on to {path}");
            }
            else
            {
                Logger.Log(LogLevel.Info, $"Unable to get relative resource for {filePath}");
            }
        }

        private static void UpdateXref(HtmlAgilityPack.HtmlNode xref, Dictionary<string, XRefSpec> internalXRefMap, Dictionary<string, XRefSpec> externalXRefMap, Func<string, string> updater, string language)
        {
            var key = xref.GetAttributeValue("href", null);
            // If name | fullName exists, use the one from xref because spec name is different from name for generic types
            // e.g. return type: IEnumerable<T>, spec name should be IEnumerable
            var name = xref.GetAttributeValue("name", null);
            var fullName = xref.GetAttributeValue("fullName", null);
            string displayName;
            string href = null;

            XRefSpec spec = null;
            if (internalXRefMap.TryGetValue(key, out spec))
            {
                href = updater(spec.Href);
                var hashtagIndex = href.IndexOf('#');
                if (hashtagIndex == -1)
                {
                    var htmlId = GetHtmlId(key);
                    // TODO: What if href is not html?
                    href = href + "#" + htmlId;
                }
            }
            else if (externalXRefMap.TryGetValue(key, out spec) && !string.IsNullOrEmpty(spec.Href))
            {
                href = spec.Href;
            }

            // If href is not null, use name
            if (href != null)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    displayName = name;
                }
                else
                {
                    displayName = string.IsNullOrEmpty(fullName) ? key : fullName;
                    if (spec != null)
                        displayName = StringHelper.HtmlEncode(GetLanguageSpecificAttribute(spec, language, displayName, "name"));
                }

                var anchorNode = $"<a class=\"xref\" href=\"{href}\">{displayName}</a>";
                xref.ParentNode.ReplaceChild(HtmlAgilityPack.HtmlNode.CreateNode(anchorNode), xref);
            }
            else
            {
                // If href is null, use fullName
                if (!string.IsNullOrEmpty(fullName))
                {
                    displayName = fullName;
                }
                else
                {
                    displayName = string.IsNullOrEmpty(name) ? key : name;
                    if (spec != null)
                        displayName = StringHelper.HtmlEncode(GetLanguageSpecificAttribute(spec, language, displayName, "fullName", "name"));
                }

                var spanNode = $"<span class=\"xref\">{displayName}</span>";
                xref.ParentNode.ReplaceChild(HtmlAgilityPack.HtmlNode.CreateNode(spanNode), xref);
            }
        }

        /// <summary>
        /// Must be consistent with template input.replace(/\W/g, '_');
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static Regex HtmlEncodeRegex = new Regex(@"\W", RegexOptions.Compiled);
        private static string GetHtmlId(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return HtmlEncodeRegex.Replace(id, "_");
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
            if (PathUtility.TryGetPathFromWorkingFolder(key, out path))
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
                    Logger.LogWarning($"File {path} is not found.");
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
            if (PathUtility.TryGetPathFromWorkingFolder(key, out path))
            {
                string xrefValue;
                if (map.TryGetValue(key, out xrefValue))
                {
                    xrefValue = updater(xrefValue);
                    link.SetAttributeValue(attribute, xrefValue);
                }
                else
                {
                    Logger.LogWarning($"File {path} is not found.");
                    // TODO: what to do if file path not exists?
                    // CURRENT: fallback to the original one
                    link.SetAttributeValue(attribute, path);
                }
            }
        }

        private static string UpdateFilePath(string path, string modelFilePathToRoot)
        {
            string pathToRoot;
            if (PathUtility.TryGetPathFromWorkingFolder(path, out pathToRoot))
            {
                return ((RelativePath)pathToRoot).MakeRelativeTo((RelativePath)modelFilePathToRoot);
            }
            return path;
        }

        public void Dispose()
        {
            _resourceProvider?.Dispose();
        }
    }
}
