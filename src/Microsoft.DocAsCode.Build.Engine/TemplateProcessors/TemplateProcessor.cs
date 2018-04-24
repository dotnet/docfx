// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class TemplateProcessor : IDisposable
    {
        private readonly ResourceFileReader _resourceProvider;
        private readonly TemplateCollection _templateCollection;
        private readonly DocumentBuildContext _context;
        private readonly int _maxParallelism;

        public IDictionary<string, string> Tokens { get; }

        /// <summary>
        /// TemplateName can be either file or folder
        /// 1. If TemplateName is file, it is considered as the default template
        /// 2. If TemplateName is a folder, files inside the folder is considered as the template, each file is named after {DocumentType}.{extension}
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="resourceProvider"></param>
        public TemplateProcessor(ResourceFileReader resourceProvider, DocumentBuildContext context, int maxParallelism = 0)
        {
            if (maxParallelism <= 0)
            {
                maxParallelism = Environment.ProcessorCount;
            }

            _context = context;
            _resourceProvider = resourceProvider;
            _maxParallelism = maxParallelism;
            _templateCollection = new TemplateCollection(resourceProvider, context, maxParallelism);
            Tokens = LoadTokenJson(resourceProvider) ?? new Dictionary<string, string>();
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

        internal List<ManifestItem> Process(List<InternalManifestItem> manifest, ApplyTemplateSettings settings, IDictionary<string, object> globals = null)
        {
            using (new LoggerPhaseScope("Apply Templates", LogLevel.Verbose))
            {
                if (globals == null)
                {
                    globals = Tokens.ToDictionary(pair => pair.Key, pair => (object)pair.Value);
                }

                if (settings == null)
                {
                    settings = _context?.ApplyTemplateSettings;
                }

                Logger.LogInfo($"Applying templates to {manifest.Count} model(s)...");
                var documentTypes = new HashSet<string>(manifest.Select(s => s.DocumentType));
                ProcessDependencies(documentTypes, settings);
                var templateManifest = ProcessCore(manifest, settings, globals);
                return templateManifest;
            }
        }

        internal void ProcessDependencies(HashSet<string> documentTypes, ApplyTemplateSettings settings)
        {
            if (settings.Options.HasFlag(ApplyTemplateOptions.TransformDocument))
            {
                var notSupportedDocumentTypes = documentTypes.Where(s => s != "Resource" && _templateCollection[s] == null).OrderBy(s => s);
                if (notSupportedDocumentTypes.Any())
                {
                    Logger.LogWarning(
                        $"There is no template processing document type(s): {StringExtension.ToDelimitedString(notSupportedDocumentTypes)}",
                        code: WarningCodes.Build.UnknownContentTypeForTemplate);
                }
                var templatesInUse = documentTypes.Select(s => _templateCollection[s]).Where(s => s != null).ToList();
                ProcessDependenciesCore(settings.OutputFolder, templatesInUse);
            }
            else
            {
                Logger.LogInfo("Dryrun, no template will be applied to the documents.");
            }
        }

        private void ProcessDependenciesCore(string outputDirectory, IEnumerable<TemplateBundle> templateBundles)
        {
            foreach (var resourceInfo in templateBundles.SelectMany(s => s.Resources).Distinct())
            {
                try
                {
                    using (var stream = _resourceProvider.GetResourceStream(resourceInfo.ResourceKey))
                    {
                        ProcessSingleDependency(stream, outputDirectory, resourceInfo.ResourceKey);
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Info, $"Unable to get relative resource for {resourceInfo.ResourceKey}: {e.Message}");
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

        private List<ManifestItem> ProcessCore(List<InternalManifestItem> items, ApplyTemplateSettings settings, IDictionary<string, object> globals)
        {
            var manifest = new ConcurrentBag<ManifestItem>();
            var transformer = new TemplateModelTransformer(_context, _templateCollection, settings, globals);
            items.RunAll(
                item =>
                {
                    using (new LoggerFileScope(item.LocalPathFromRoot))
                    {
                        manifest.Add(transformer.Transform(item));
                    }
                },
                _maxParallelism);
            return manifest.ToList();

        }

        private static IDictionary<string, string> LoadTokenJson(ResourceFileReader resource)
        {
            var tokenJson = resource.GetResource("token.json");
            if (string.IsNullOrEmpty(tokenJson))
            {
                // also load `global.json` for backward compatibility
                // TODO: remove this
                tokenJson = resource.GetResource("global.json");
                if (string.IsNullOrEmpty(tokenJson))
                {
                    return null;
                }
            }

            return JsonUtility.FromJsonString<Dictionary<string, string>>(tokenJson);
        }

        public void Dispose()
        {
            _resourceProvider?.Dispose();
        }
    }
}
