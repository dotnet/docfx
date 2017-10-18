// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class HostServiceCreator : IHostServiceCreator
    {
        private DocumentBuildContext _context;

        public HostServiceCreator(DocumentBuildContext context)
        {
            _context = context;
        }

        public virtual bool CanProcessorIncremental(IDocumentProcessor processor)
        {
            return false;
        }

        public virtual bool ShouldProcessorTraceInfo(IDocumentProcessor processor)
        {
            return false;
        }

        public virtual HostService CreateHostService(
            DocumentBuildParameters parameters,
            TemplateProcessor templateProcessor,
            IMarkdownService markdownService,
            IEnumerable<IInputMetadataValidator> metadataValidator,
            IDocumentProcessor processor,
            IEnumerable<FileAndType> files,
            ImmutableDictionary<FileAndType, FileModel> preloadOverwrites = null)
        {
            var hostService = new HostService(
                parameters.Files.DefaultBaseDir,
                LoadFileModels(processor, parameters, files, preloadOverwrites),
                parameters.VersionName,
                parameters.VersionDir,
                parameters.LruSize,
                parameters.GroupInfo)
            {
                MarkdownService = markdownService,
                Processor = processor,
                Template = templateProcessor,
                Validators = metadataValidator?.ToImmutableList(),
                ShouldTraceIncrementalInfo = ShouldProcessorTraceInfo(processor),
                CanIncrementalBuild = CanProcessorIncremental(processor),
            };
            return hostService;
        }

        public virtual FileModel Load(IDocumentProcessor processor, ImmutableDictionary<string, object> metadata, FileMetadata fileMetadata, FileAndType file)
        {
            using (new LoggerFileScope(file.File))
            {
                Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}: Loading...");

                var path = Path.Combine(file.BaseDir, file.File);
                metadata = ApplyFileMetadata(path, metadata, fileMetadata);
                try
                {
                    return processor.Load(file, metadata);
                }
                catch (Exception)
                {
                    Logger.LogError($"Unable to load file: {file.File} via processor: {processor.Name}.");
                    throw;
                }
            }
        }
        private IEnumerable<FileModel> LoadFileModels(
            IDocumentProcessor processor,
            DocumentBuildParameters parameters,
            IEnumerable<FileAndType> files,
            ImmutableDictionary<FileAndType, FileModel> preloadOverwrites)
        {
            if (files == null)
            {
                yield break;
            }

            foreach (var file in files)
            {
                if (preloadOverwrites != null && file.Type == DocumentType.Overwrite)
                {
                    if (preloadOverwrites.TryGetValue(file, out var model))
                    {
                        yield return model;
                    }
                    else
                    {
                        throw new InvalidOperationException($"{file.Type}:{file.FullPath} should exist but it is not.");
                    }
                }
                else
                {
                    var model = Load(processor, parameters.Metadata, parameters.FileMetadata, file);
                    if (model != null)
                    {
                        yield return model;
                    }
                }
            }
        }

        private static ImmutableDictionary<string, object> ApplyFileMetadata(
            string file,
            ImmutableDictionary<string, object> metadata,
            FileMetadata fileMetadata)
        {
            if (fileMetadata == null || fileMetadata.Count == 0) return metadata;
            var result = new Dictionary<string, object>(metadata);
            var baseDir = string.IsNullOrEmpty(fileMetadata.BaseDir) ? Directory.GetCurrentDirectory() : fileMetadata.BaseDir;
            var relativePath = PathUtility.MakeRelativePath(baseDir, file);
            foreach (var item in fileMetadata)
            {
                // As the latter one overrides the former one, match the pattern from latter to former
                for (int i = item.Value.Length - 1; i >= 0; i--)
                {
                    if (item.Value[i].Glob.Match(relativePath))
                    {
                        // override global metadata if metadata is defined in file metadata
                        result[item.Value[i].Key] = item.Value[i].Value;
                        Logger.LogVerbose($"{relativePath} matches file metadata with glob pattern {item.Value[i].Glob.Raw} for property {item.Value[i].Key}");
                        break;
                    }
                }
            }
            return result.ToImmutableDictionary();
        }
    }
}
