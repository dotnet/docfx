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

        public static string UpdateFilePath(string path, string documentType, TemplateCollection templateCollection)
        {
            if (templateCollection == null) return path;
            var templates = templateCollection[documentType];

            // Get default template extension
            if (templates == null || templates.Count == 0) return path;

            var defaultTemplate = templates.FirstOrDefault(s => s.IsPrimary) ?? templates[0];
            return Path.ChangeExtension(path, defaultTemplate.Extension);
        }

        // TODO: change to use IDocumentBuildContext
        public static TemplateManifestItem Transform(DocumentBuildContext context, ManifestItem item, TemplateCollection templateCollection, string outputDirectory, bool exportMetadata, Func<string, string> metadataFilePathProvider)
        {
            if (item.Model == null || item.Model.Content == null) throw new ArgumentNullException("Content for item.Model should not be null!");
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
                var model = item.Model.Content;
                var templates = templateCollection[item.DocumentType];
                // 1. process model
                if (templates == null)
                {
                    // Logger.LogWarning($"There is no template processing {item.DocumentType} document \"{item.LocalPathFromRepoRoot}\"");
                }
                else
                {
                    var systemAttrs = new SystemAttributes(context, item, TemplateProcessor.Language);
                    foreach (var template in templates)
                    {
                        var extension = template.Extension;
                        string outputFile = Path.ChangeExtension(item.ModelFile, extension);
                        string outputPath = Path.Combine(outputDirectory ?? string.Empty, outputFile);
                        var dir = Path.GetDirectoryName(outputPath);
                        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                        string transformed;
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
                Logger.LogError($"Unable to transform {item.ModelFile}: {e.Message}. Ignored.");
                throw;
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

        // TODO: move uid resolve error here
        private static void UpdateXref(HtmlAgilityPack.HtmlNode node, Dictionary<string, XRefSpec> internalXRefMap, Dictionary<string, XRefSpec> externalXRefMap, Func<string, string> updater, string language)
        {
            var xref = XrefDetails.From(node);

            // Resolve external xref map first, and then internal xref map.
            // Internal one overrides external one
            bool resolved = xref.TryResolve(externalXRefMap, null);
            resolved = xref.TryResolve(internalXRefMap, updater);

            var convertedNode = xref.ConvertToHtmlNode(language);
            node.ParentNode.ReplaceChild(convertedNode, node);
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
