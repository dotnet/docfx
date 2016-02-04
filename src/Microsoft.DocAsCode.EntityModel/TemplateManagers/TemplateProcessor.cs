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
    using System.Web;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.Exceptions;
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

        public string UpdateFileExtension(string path, string documentType)
        {
            if (IsEmpty) return path;
            var templates = Templates[documentType];

            // Get default template extension
            if (templates == null || templates.Count == 0) return path;

            var defaultTemplate = templates.FirstOrDefault(s => s.IsPrimary) ?? templates[0];
            return Path.ChangeExtension(path, defaultTemplate.Extension);
        }

        public static void Transform(TemplateProcessor processor, List<ManifestItem> manifest, DocumentBuildContext context, ApplyTemplateSettings settings)
        {
            if (settings.Options == ApplyTemplateOptions.ExportRawModel || processor == null)
            {
                ExportRawModel(manifest, settings);
                return;
            }

            using (new LoggerPhaseScope("Apply Templates"))
            {
                Logger.LogInfo($"Applying templates to {manifest.Count} model(s)...");

                processor.ProcessDependencies(settings.OutputFolder);
                if (processor.IsEmpty)
                {
                    Logger.LogWarning("No template is found.");
                    ExportRawModel(manifest, settings);
                    return;
                }

                Logger.LogVerbose("Start applying template...");

                var outputDirectory = context.BuildOutputFolder;

                var templateManifest = processor.Transform(manifest, context, settings).ToList();

                if (!settings.Options.HasFlag(ApplyTemplateOptions.TransformDocument))
                {
                    Logger.LogInfo("Dryrun, no template will be applied to the documents.");
                }

                if (templateManifest.Count > 0)
                {
                    // Save manifest from template
                    var manifestPath = Path.Combine(outputDirectory ?? string.Empty, ManifestFileName);
                    JsonUtility.Serialize(manifestPath, templateManifest);
                    Logger.LogInfo($"Manifest file saved to {manifestPath}.");
                }
            }
        }

        public IEnumerable<TemplateManifestItem> Transform(IEnumerable<ManifestItem> items, DocumentBuildContext context, ApplyTemplateSettings settings)
        {
            var documentTypes = items.Select(s => s.DocumentType).Distinct().Where(s => s != "Resource" && Templates[s] == null);
            if (documentTypes.Any())
            {
                Logger.LogWarning($"There is no template processing document type(s): {documentTypes.ToDelimitedString()}");
            }

            foreach (var item in items)
            {
                var manifestItem = TransformItem(item, context, settings);
                if (manifestItem != null) yield return manifestItem;
            }
        }

        private static void ExportRawModel(List<ManifestItem> manifest, ApplyTemplateSettings settings)
        {
            if (!settings.Options.HasFlag(ApplyTemplateOptions.ExportRawModel)) return;
            Logger.LogInfo($"Exporting {manifest.Count} raw model(s)...");
            foreach (var item in manifest)
            {
                ExportModel(item.Model.Content, item.ModelFile, settings.RawModelExportSettings);
            }
        }

        private static void ExportModel(object model, string modelFileRelativePath, ExportSettings settings)
        {
            if (model == null) return;
            var outputFolder = settings.OutputFolder;

            string rawModelPath = Path.Combine(outputFolder ?? string.Empty, settings.PathRewriter(modelFileRelativePath));

            JsonUtility.Serialize(rawModelPath, model);
        }

        private TemplateManifestItem TransformItem(ManifestItem item, IDocumentBuildContext context, ApplyTemplateSettings settings)
        {
            if (settings.Options.HasFlag(ApplyTemplateOptions.ExportRawModel))
            {
                ExportModel(item.Model.Content, item.ModelFile, settings.RawModelExportSettings);
            }

            if (item.Model == null || item.Model.Content == null) throw new ArgumentNullException("Content for item.Model should not be null!");
            var manifestItem = new TemplateManifestItem
            {
                DocumentType = item.DocumentType,
                OriginalFile = item.LocalPathFromRepoRoot,
                OutputFiles = new Dictionary<string, string>()
            };
            var outputDirectory = settings.OutputFolder ?? Environment.CurrentDirectory;
            if (!IsEmpty)
            {
                HashSet<string> missingUids = new HashSet<string>();
                try
                {
                    var model = item.Model.Content;
                    var templates = Templates[item.DocumentType];

                    // 1. process model
                    if (templates != null)
                    {
                        var systemAttrs = new SystemAttributes(context, item, TemplateProcessor.Language);
                        foreach (var template in templates)
                        {
                            var extension = template.Extension;
                            string outputFile = Path.ChangeExtension(item.ModelFile, extension);
                            string outputPath = Path.Combine(outputDirectory, outputFile);
                            var dir = Path.GetDirectoryName(outputPath);
                            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                            var result = template.TransformModel(model, systemAttrs);

                            if (settings.Options.HasFlag(ApplyTemplateOptions.ExportViewModel))
                            {
                                ExportModel(result.Model, outputFile, settings.ViewModelExportSettings);
                            }

                            if (settings.Options.HasFlag(ApplyTemplateOptions.TransformDocument))
                            {
                                string transformed = result.Result;
                                if (!string.IsNullOrWhiteSpace(transformed))
                                {
                                    if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            TranformHtml(context, transformed, item.ModelFile, outputPath);
                                        }
                                        catch (AggregateException e)
                                        {
                                            e.Handle(s =>
                                            {
                                                var xrefExcetpion = s as CrossReferenceNotResolvedException;
                                                if (xrefExcetpion != null)
                                                {
                                                    missingUids.Add(xrefExcetpion.UidRawText);
                                                    return true;
                                                }
                                                else
                                                {
                                                    return false;
                                                }
                                            });
                                        }
                                    }
                                    else
                                    {
                                        File.WriteAllText(outputPath, transformed, Encoding.UTF8);
                                    }

                                    Logger.Log(LogLevel.Verbose, $"Transformed model \"{item.LocalPathFromRepoRoot}\" to \"{outputPath}\".");
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
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError($"Unable to transform {item.ModelFile}: {e.Message}. Ignored.");
                    throw;
                }

                if (missingUids.Count > 0)
                {
                    var uids = string.Join(", ", missingUids.Select(s => $"\"{s}\""));
                    Logger.LogWarning($"Unable to resolve cross-reference {uids} for \"{manifestItem.OriginalFile.ToDisplayPath()}\"");
                }
            }

            // 2. process resource
            if (item.ResourceFile != null)
            {
                PathUtility.CopyFile(Path.Combine(item.InputFolder, item.ResourceFile), Path.Combine(outputDirectory, item.ResourceFile), true);
                manifestItem.OutputFiles.Add("resource", item.ResourceFile);
            }

            return manifestItem;
        }

        private static void TranformHtml(IDocumentBuildContext context, string transformed, string relativeModelPath, string outputPath)
        {
            // Update HREF and XREF
            HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
            html.LoadHtml(transformed);

            var xrefExceptions = new List<CrossReferenceNotResolvedException>();
            var xrefNodes = html.DocumentNode.SelectNodes("//xref/@href");
            if (xrefNodes != null)
            {
                foreach(var xref in xrefNodes)
                {
                    try
                    {
                        UpdateXref(xref, context, Language);
                    }
                    catch (CrossReferenceNotResolvedException e)
                    {
                        xrefExceptions.Add(e);
                    }
                }
            }

            var srcNodes = html.DocumentNode.SelectNodes("//*/@src");
            if (srcNodes != null)
                foreach (var link in srcNodes)
                {
                    UpdateHref(link, "src", context, relativeModelPath);
                }

            var hrefNodes = html.DocumentNode.SelectNodes("//*/@href");
            if (hrefNodes != null)
            {
                foreach (var link in hrefNodes)
                {
                    UpdateHref(link, "href", context, relativeModelPath);
                }
            }

            // Save with extension changed
            var subDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(subDirectory) && !Directory.Exists(subDirectory)) Directory.CreateDirectory(subDirectory);
            html.Save(outputPath, Encoding.UTF8);
            if (xrefExceptions.Count > 0)
            {
                throw new AggregateException(xrefExceptions);
            }
        }

        private void ProcessDependencies(string outputDirectory)
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

        private static void UpdateXref(HtmlAgilityPack.HtmlNode node, IDocumentBuildContext context, string language)
        {
            var xref = XrefDetails.From(node);

            // Resolve external xref map first, and then internal xref map.
            // Internal one overrides external one
            var xrefSpec = context.GetXrefSpec(xref.Uid);
            xref.ApplyXrefSpec(xrefSpec);
            bool resolved = xrefSpec != null;

            var convertedNode = xref.ConvertToHtmlNode(language);
            node.ParentNode.ReplaceChild(convertedNode, node);
            if (!resolved)
            {
                if (xref.ThrowIfNotResolved)
                {
                    throw new CrossReferenceNotResolvedException(xref.Uid, xref.Raw, null);
                }
            }
        }

        private static void UpdateHref(HtmlAgilityPack.HtmlNode link, string attribute, IDocumentBuildContext context, string relativePath)
        {
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

                href = context.GetFilePath(HttpUtility.UrlDecode(key));
                if (href != null)
                {
                    href = UpdateFilePath(href, relativePath);
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
