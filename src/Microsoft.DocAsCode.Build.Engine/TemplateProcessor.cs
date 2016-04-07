// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class TemplateProcessor : IDisposable
    {
        private readonly ResourceCollection _resourceProvider;
        private readonly object _global;

        private readonly TemplateCollection _templateCollection;

        public static List<TemplateManifestItem> Process(TemplateProcessor processor, List<ManifestItem> manifest, DocumentBuildContext context, ApplyTemplateSettings settings)
        {
            if (processor == null)
            {
                processor = new TemplateProcessor(new EmptyResourceCollection(), 1);
            }

            return processor.Process(manifest, context, settings);
        }

        /// <summary>
        /// TemplateName can be either file or folder
        /// 1. If TemplateName is file, it is considered as the default template
        /// 2. If TemplateName is a folder, files inside the folder is considered as the template, each file is named after {DocumentType}.{extension}
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="resourceProvider"></param>
        public TemplateProcessor(ResourceCollection resourceProvider, int maxParallelism = 0)
        {
            if (maxParallelism <= 0)
            {
                maxParallelism = Environment.ProcessorCount;
            }

            _resourceProvider = resourceProvider;
            _global = LoadGlobalJson(resourceProvider);
            _templateCollection = new TemplateCollection(resourceProvider, maxParallelism);
        }

        public string UpdateFileExtension(string path, string documentType)
        {
            if (_templateCollection.Count == 0) return path;
            var templates = _templateCollection[documentType];

            // Get default template extension
            if (templates == null || templates.Count == 0) return path;

            var defaultTemplate = templates.FirstOrDefault(s => s.IsPrimary) ?? templates[0];
            return Path.ChangeExtension(path, defaultTemplate.Extension);
        }

        public List<TemplateManifestItem> Process(List<ManifestItem> manifest, DocumentBuildContext context, ApplyTemplateSettings settings)
        {
            using (new LoggerPhaseScope("Apply Templates"))
            {
                var documentTypes = manifest.Select(s => s.DocumentType).Distinct();
                var notSupportedDocumentTypes = documentTypes.Where(s => s != "Resource" && _templateCollection[s] == null);
                if (notSupportedDocumentTypes.Any())
                {
                    Logger.LogWarning($"There is no template processing document type(s): {notSupportedDocumentTypes.ToDelimitedString()}");
                }
                Logger.LogInfo($"Applying templates to {manifest.Count} model(s)...");

                if (settings.Options.HasFlag(ApplyTemplateOptions.TransformDocument))
                {
                    var templatesInUse = documentTypes.Select(s => _templateCollection[s]).Where(s => s != null).SelectMany(s => s).ToList();
                    ProcessDependencies(settings.OutputFolder, templatesInUse);
                }
                else
                {
                    Logger.LogInfo("Dryrun, no template will be applied to the documents.");
                }

                var outputDirectory = context.BuildOutputFolder;

                var templateManifest = ProcessCore(manifest, context, settings);
                SaveManifest(templateManifest, outputDirectory, context);
                return templateManifest;
            }
        }

        private void ProcessDependencies(string outputDirectory, IEnumerable<Template> templates)
        {
            foreach (var resourceInfo in templates.SelectMany(s => s.Resources).Distinct())
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

        private List<TemplateManifestItem> ProcessCore(List<ManifestItem> items, DocumentBuildContext context, ApplyTemplateSettings settings)
        {
            var manifest = new ConcurrentBag<TemplateManifestItem>();
            var systemAttributeGenerator = new SystemMetadataGenerator(context);
            items.RunAll(
                item =>
                {
                    var manifestItem = ProcessItem(item, context, settings, systemAttributeGenerator);
                    manifest.Add(manifestItem);
                },
                context.MaxParallelism);

            return manifest.ToList();
        }

        private TemplateManifestItem ProcessItem(ManifestItem item, IDocumentBuildContext context, ApplyTemplateSettings settings, SystemMetadataGenerator systemAttributeGenerator)
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

            // 1. process resource
            if (item.ResourceFile != null)
            {
                // Resource file has already been processed in its plugin
                manifestItem.OutputFiles.Add("resource", item.ResourceFile);
            }

            // 2. process model
            var templates = _templateCollection[item.DocumentType];
            if (templates == null || templates.Count == 0)
            {
                return manifestItem;
            }

            HashSet<string> missingUids = new HashSet<string>();

            // Must convert to JObject first as we leverage JsonProperty as the property name for the model
            var model = ConvertToObjectHelper.ConvertStrongTypeToJObject(item.Model.Content);
            var systemAttrs = systemAttributeGenerator.Generate(item);
            foreach (var template in templates)
            {
                var extension = template.Extension;
                string outputFile = Path.ChangeExtension(item.ModelFile, extension);
                string outputPath = Path.Combine(outputDirectory, outputFile);
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                object viewModel = null;
                try
                {
                    viewModel = template.TransformModel(model, systemAttrs, _global);
                }
                catch (Exception e)
                {
                    // save raw model for further investigation:
                    var exportSettings = ApplyTemplateSettings.RawModelExportSettingsForDebug;
                    var rawModelPath = ExportModel(model, item.ModelFile, exportSettings);
                    var message = $"Error transforming model \"{rawModelPath}\" generated from \"{item.LocalPathFromRepoRoot}\" using \"{template.ScriptName}\": {e.Message}";
                    Logger.LogError(message);
                    throw new DocumentException(message, e);
                }

                string result;
                try
                {
                    result = template.Transform(viewModel);
                }
                catch (Exception e)
                {
                    // save view model for further investigation:
                    var exportSettings = ApplyTemplateSettings.ViewModelExportSettingsForDebug;
                    var viewModelPath = ExportModel(viewModel, outputFile, exportSettings);
                    var message = $"Error applying template \"{template.Name}\" to view model \"{viewModelPath}\" generated from \"{item.LocalPathFromRepoRoot}\": {e.Message}";
                    Logger.LogError(message);
                    throw new DocumentException(message, e);
                }

                if (settings.Options.HasFlag(ApplyTemplateOptions.ExportViewModel))
                {
                    ExportModel(viewModel, outputFile, settings.ViewModelExportSettings);
                }

                if (settings.Options.HasFlag(ApplyTemplateOptions.TransformDocument))
                {
                    if (string.IsNullOrWhiteSpace(result))
                    {
                        // TODO: WHAT to do if is transformed to empty string? STILL creat empty file?
                        Logger.LogWarning($"Model \"{item.ModelFile}\" is transformed to empty string with template \"{template.Name}\"");
                        File.WriteAllText(outputPath, string.Empty);
                    }
                    else
                    {
                        TransformDocument(result, extension, context, outputPath, item.ModelFile, missingUids);
                        Logger.Log(LogLevel.Verbose, $"Transformed model \"{item.LocalPathFromRepoRoot}\" to \"{outputPath}\".");
                    }

                    manifestItem.OutputFiles.Add(extension, outputFile);
                }
            }

            if (missingUids.Count > 0)
            {
                var uids = string.Join(", ", missingUids.Select(s => $"\"{s}\""));
                Logger.LogWarning($"Unable to resolve cross-reference {uids}");
            }

            return manifestItem;
        }

        private static object LoadGlobalJson(ResourceCollection resource)
        {
            var globalJson = resource.GetResource("global.json");
            if (!string.IsNullOrEmpty(globalJson))
            {
                return JsonUtility.FromJsonString<object>(globalJson);
            }
            return null;
        }

        private static void SaveManifest(List<TemplateManifestItem> templateManifest, string outputDirectory, IDocumentBuildContext context)
        {
            // Save manifest from template
            // TODO: Keep .manifest for backward-compatability, will remove next sprint
            var manifestPath = Path.Combine(outputDirectory ?? string.Empty, Constants.ObsoleteManifestFileName);
            JsonUtility.Serialize(manifestPath, templateManifest);
            // Logger.LogInfo($"Manifest file saved to {manifestPath}. NOTE: This file is out-of-date and will be removed in version 1.8, if you rely on this file, please change to use {Constants.ManifestFileName} instead.");

            var manifestJsonPath = Path.Combine(outputDirectory ?? string.Empty, Constants.ManifestFileName);

            var toc = context.GetTocInfo();
            var manifestObject = GenerateManifest(context, templateManifest);
            JsonUtility.Serialize(manifestJsonPath, manifestObject);
            Logger.LogInfo($"Manifest file saved to {manifestJsonPath}.");
        }

        private static Manifest GenerateManifest(IDocumentBuildContext context, List<TemplateManifestItem> items)
        {
            var toc = context.GetTocInfo();
            var homepages = toc
                .Where(s => !string.IsNullOrEmpty(s.Homepage))
                .Select(s => new HomepageInfo
                {
                    Homepage = RelativePath.GetPathWithoutWorkingFolderChar(s.Homepage),
                    TocPath = RelativePath.GetPathWithoutWorkingFolderChar(context.GetFilePath(s.TocFileKey))
                }).ToList();
            return new Manifest
            {
                Homepages = homepages,
                Files = items,
            };
        }

        private static string ExportModel(object model, string modelFileRelativePath, ExportSettings settings)
        {
            if (model == null) return null;
            var outputFolder = settings.OutputFolder;

            string modelPath = Path.Combine(outputFolder ?? string.Empty, settings.PathRewriter(modelFileRelativePath));

            JsonUtility.Serialize(modelPath, model);
            return modelPath.ToDisplayPath();
        }

        private static void TransformDocument(string result, string extension, IDocumentBuildContext context, string outputPath, string relativeOutputPath, HashSet<string> missingUids)
        {
            if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    TranformHtml(context, result, relativeOutputPath, outputPath);
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
                File.WriteAllText(outputPath, result, Encoding.UTF8);
            }
        }

        private static void TranformHtml(IDocumentBuildContext context, string transformed, string relativeModelPath, string outputPath)
        {
            // Update HREF and XREF
            HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
            html.LoadHtml(transformed);

            var xrefLinkNodes = html.DocumentNode.SelectNodes("//a[starts-with(@href, 'xref:')]");
            if (xrefLinkNodes != null)
            {
                foreach (var xref in xrefLinkNodes)
                {
                    TransformXrefLink(xref, context);
                }
            }

            var xrefExceptions = new List<CrossReferenceNotResolvedException>();
            var xrefNodes = html.DocumentNode.SelectNodes("//xref/@href");
            if (xrefNodes != null)
            {
                foreach(var xref in xrefNodes)
                {
                    try
                    {
                        UpdateXref(xref, context, Constants.DefaultLanguage);
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

        private static void TransformXrefLink(HtmlAgilityPack.HtmlNode node, IDocumentBuildContext context)
        {
            var convertedNode = XrefDetails.ConvertXrefLinkNodeToXrefNode(node);
            node.ParentNode.ReplaceChild(convertedNode, node);
        }

        private static void UpdateXref(HtmlAgilityPack.HtmlNode node, IDocumentBuildContext context, string language)
        {
            var xref = XrefDetails.From(node);

            // Resolve external xref map first, and then internal xref map.
            // Internal one overrides external one
            var xrefSpec = context.GetXrefSpec(xref.Uid);
            xref.ApplyXrefSpec(xrefSpec);

            var convertedNode = xref.ConvertToHtmlNode(language);
            node.ParentNode.ReplaceChild(convertedNode, node);
            if (xrefSpec == null)
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
            if (RelativePath.TryGetPathWithoutWorkingFolderChar(key, out path))
            {
                string href;

                href = context.GetFilePath(key);
                if (href != null)
                {
                    href = ((RelativePath)UpdateFilePath(href, relativePath)).UrlEncode();

                    var anchor = link.GetAttributeValue("anchor", null);
                    if (!string.IsNullOrEmpty(anchor))
                    {
                        href += anchor;
                        link.Attributes.Remove(link.Attributes["anchor"]);
                    }
                    link.SetAttributeValue(attribute, href);
                }
                else
                {
                    // Logger.LogWarning($"File {path} is not found in {relativePath}.");
                    // TODO: what to do if file path not exists?
                    // CURRENT: fallback to the original one
                    link.SetAttributeValue(attribute, path);
                }
            }
        }

        private static string UpdateFilePath(string path, string modelFilePathToRoot)
        {
            if (RelativePath.IsPathFromWorkingFolder(path))
            {
                return ((RelativePath)path).RemoveWorkingFolder().MakeRelativeTo((RelativePath)modelFilePathToRoot);
            }

            return path;
        }

        public void Dispose()
        {
            _resourceProvider?.Dispose();
        }
    }
}
