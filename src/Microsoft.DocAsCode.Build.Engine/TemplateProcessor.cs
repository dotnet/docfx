// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class TemplateProcessor : IDisposable
    {
        private readonly ResourceCollection _resourceProvider;

        private readonly TemplateCollection _templateCollection;

        public static readonly TemplateProcessor DefaultProcessor = new TemplateProcessor(new EmptyResourceCollection(), 1);

        public IDictionary<string, object> DefaultGlobalVariables { get; }

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
            _templateCollection = new TemplateCollection(resourceProvider, maxParallelism);
            DefaultGlobalVariables = LoadGlobalJson(resourceProvider) ?? new Dictionary<string, object>();
        }

        public TemplateBundle GetTemplateBundle(string documentType)
        {
            if (string.IsNullOrEmpty(documentType)) throw new ArgumentNullException(nameof(documentType));
            return _templateCollection[documentType];
        }

        public bool TryGetFileExtension(string documentType, out string fileExtension)
        {
            if (string.IsNullOrEmpty(documentType)) throw new ArgumentNullException(nameof(documentType));
            fileExtension = string.Empty;
            if (_templateCollection.Count == 0) return false;
            var templateBundle = _templateCollection[documentType];

            // Get default template extension
            if (templateBundle == null) return false;

            fileExtension = templateBundle.Extension;
            return true;
        }

        public List<TemplateManifestItem> Process(List<ManifestItem> manifest, DocumentBuildContext context, ApplyTemplateSettings settings, IDictionary<string, object> globals = null)
        {
            using (new LoggerPhaseScope("Apply Templates"))
            {
                if (globals == null)
                {
                    globals = DefaultGlobalVariables;
                }

                var documentTypes = manifest.Select(s => s.DocumentType).Distinct();
                var notSupportedDocumentTypes = documentTypes.Where(s => s != "Resource" && _templateCollection[s] == null);
                if (notSupportedDocumentTypes.Any())
                {
                    Logger.LogWarning($"There is no template processing document type(s): {notSupportedDocumentTypes.ToDelimitedString()}");
                }
                Logger.LogInfo($"Applying templates to {manifest.Count} model(s)...");

                if (settings.Options.HasFlag(ApplyTemplateOptions.TransformDocument))
                {
                    var templatesInUse = documentTypes.Select(s => _templateCollection[s]).Where(s => s != null).ToList();
                    ProcessDependencies(settings.OutputFolder, templatesInUse);
                }
                else
                {
                    Logger.LogInfo("Dryrun, no template will be applied to the documents.");
                }

                var outputDirectory = context.BuildOutputFolder;

                var templateManifest = ProcessCore(manifest, context, settings, globals);
                SaveManifest(templateManifest, outputDirectory, context);
                return templateManifest;
            }
        }

        private void ProcessDependencies(string outputDirectory, IEnumerable<TemplateBundle> templateBundles)
        {
            foreach (var resourceInfo in templateBundles.SelectMany(s => s.Resources).Distinct())
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

        private List<TemplateManifestItem> ProcessCore(List<ManifestItem> items, DocumentBuildContext context, ApplyTemplateSettings settings, IDictionary<string, object> globals)
        {
            var manifest = new ConcurrentBag<TemplateManifestItem>();
            var systemAttributeGenerator = new SystemMetadataGenerator(context);
            var transformer = new TemplateModelTransformer(context, _templateCollection, settings, globals);
            items.RunAll(
                item =>
                {
                    var manifestItem = transformer.Transform(item);
                    if (manifestItem.OutputFiles?.Count > 0)
                    {
                        manifest.Add(manifestItem);
                    }
                },
                context.MaxParallelism);

            var itemsToRemove = new List<string>();
            foreach (var duplicates in from m in manifest
                                       from output in m.OutputFiles.Values
                                       group m.OriginalFile by output into g
                                       where g.Count() > 1
                                       select g)
            {
                Logger.LogWarning($"Overwrite occurs while input files \"{string.Join(", ", duplicates)}\" writing to the same output file \"{duplicates.Key}\"");
                itemsToRemove.AddRange(duplicates.Skip(1));
            }

            return manifest.Where(m => !itemsToRemove.Contains(m.OriginalFile)).ToList();
        }

        private static IDictionary<string, object> LoadGlobalJson(ResourceCollection resource)
        {
            var globalJson = resource.GetResource("global.json");
            if (string.IsNullOrEmpty(globalJson))
            {
                return null;
            }

            return JsonUtility.FromJsonString<Dictionary<string, object>>(globalJson);
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
                XRefMap = DocumentBuilder.XRefMapFileName,
            };
        }

        public void Dispose()
        {
            _resourceProvider?.Dispose();
        }
    }
}
