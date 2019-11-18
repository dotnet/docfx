// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Newtonsoft.Json.Linq;

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
            IEnumerable<FileAndType> files)
        {
            var (models, invalidFiles) = LoadModels(files, parameters, processor);
            var hostService = new HostService(
                parameters.Files.DefaultBaseDir,
                models,
                parameters.VersionName,
                parameters.VersionDir,
                parameters.LruSize,
                parameters.GroupInfo,
                new BuildParameters(parameters.TagParameters))
            {
                MarkdownService = markdownService,
                Processor = processor,
                Template = templateProcessor,
                Validators = metadataValidator?.ToImmutableList(),
                ShouldTraceIncrementalInfo = ShouldProcessorTraceInfo(processor),
                CanIncrementalBuild = CanProcessorIncremental(processor),
                InvalidSourceFiles = invalidFiles.ToImmutableList(),
            };
            return hostService;
        }

        public virtual (FileModel model, bool valid) Load(
            IDocumentProcessor processor,
            ImmutableDictionary<string, object> metadata,
            FileMetadata fileMetadata,
            FileAndType file)
        {
            using (new LoggerFileScope(file.File))
            {
                Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}: Loading...");

                var fileMeta = NeedApplyMetadata()
                    ? ApplyFileMetadata(file.FullPath, metadata, fileMetadata)
                    : ImmutableDictionary<string, object>.Empty;
                try
                {
                    return (processor.Load(file, fileMeta), true);
                }
                catch (Exception e)
                {
                    Logger.LogError(
                        $"Unable to load file '{file.File}' via processor '{processor.Name}': {e.Message}",
                        code: ErrorCodes.Build.InvalidInputFile);
                    return (null, false);
                }
            }

            bool NeedApplyMetadata()
            {
                return file.Type != DocumentType.Resource;
            }
        }

        private (IEnumerable<FileModel> models, IEnumerable<string> invalidFiles) LoadModels(IEnumerable<FileAndType> files, DocumentBuildParameters parameters, IDocumentProcessor processor)
        {
            if (files == null)
            {
                return (Enumerable.Empty<FileModel>(), Enumerable.Empty<string>());
            }

            var models = new ConcurrentBag<FileModel>();
            var invalidFiles = new ConcurrentBag<string>();
            files.RunAll(file =>
            {
                var (model, valid) = Load(processor, parameters.Metadata, parameters.FileMetadata, file);
                if (model != null)
                {
                    models.Add(model);
                }
                if (!valid)
                {
                    invalidFiles.Add(file.File);
                }
            }, _context.MaxParallelism);

            return (models, invalidFiles);
        }

        private static ImmutableDictionary<string, object> ApplyFileMetadata(
            string file,
            ImmutableDictionary<string, object> metadata,
            FileMetadata fileMetadata)
        {
            if (fileMetadata == null || fileMetadata.Count == 0)
            {
                return metadata;
            }

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
                        Logger.LogDiagnostic($"{relativePath} matches file metadata with glob pattern {item.Value[i].Glob.Raw} for property {item.Value[i].Key}");
                        break;
                    }
                }
            }
            return result.ToImmutableDictionary();
        }

        private sealed class BuildParameters : IBuildParameters
        {
            public IReadOnlyDictionary<string, JArray> TagParameters { get; }

            public BuildParameters(IReadOnlyDictionary<string, JArray> tagParameters)
            {
                TagParameters = tagParameters;
            }
        }
    }
}
