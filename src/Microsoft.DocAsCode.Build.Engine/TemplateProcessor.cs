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
            var transformer = new TemplateModelTransformer(context, _templateCollection, settings, _global);
            items.RunAll(
                item =>
                {
                    var manifestItem = transformer.Transform(item);
                    manifest.Add(manifestItem);
                },
                context.MaxParallelism);

            return manifest.ToList();
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

        public void Dispose()
        {
            _resourceProvider?.Dispose();
        }
    }
}
