﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class HostServiceCreatorWithIncremental : HostServiceCreator
    {
        private readonly ConcurrentDictionary<string, Lazy<bool>> _cache = new ConcurrentDictionary<string, Lazy<bool>>();

        public IncrementalBuildContext IncrementalContext { get; }

        public HostServiceCreatorWithIncremental(DocumentBuildContext context) : base(context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            IncrementalContext = context.IncrementalBuildContext;
        }

        public override bool ShouldProcessorTraceInfo(IDocumentProcessor processor)
        {
            var key = $"trace-{processor.Name}";
            return _cache.GetOrAdd(key, k => new Lazy<bool>(() => ShouldProcessorTraceInfoCore(processor))).Value;
        }

        public override bool CanProcessorIncremental(IDocumentProcessor processor)
        {
            var key = $"canIncremental-{processor.Name}";
            return _cache.GetOrAdd(key, k => new Lazy<bool>(() => CanProcessorIncrementalCore(processor))).Value;
        }

        public override HostService CreateHostService(
            DocumentBuildParameters parameters,
            TemplateProcessor templateProcessor,
            IMarkdownService markdownService,
            IEnumerable<IInputMetadataValidator> metadataValidator,
            IDocumentProcessor processor,
            IEnumerable<FileAndType> files)
        {
            if (ShouldProcessorTraceInfo(processor))
            {
                IncrementalContext.CreateProcessorInfo(processor);
            }
            var hs = base.CreateHostService(parameters, templateProcessor, markdownService, metadataValidator, processor, files);
            PostCreate(hs, files);
            return hs;
        }

        public override FileModel Load(IDocumentProcessor processor, ImmutableDictionary<string, object> metadata, FileMetadata fileMetadata, FileAndType file)
        {
            using (new LoggerFileScope(file.File))
            {
                if (CanProcessorIncremental(processor))
                {
                    ChangeKindWithDependency ck;
                    string fileKey = ((RelativePath)file.File).GetPathFromWorkingFolder().ToString();
                    if (IncrementalContext.ChangeDict.TryGetValue(fileKey, out ck))
                    {
                        Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}, ChangeType {ck}.");
                        if (ck == ChangeKindWithDependency.Deleted)
                        {
                            return null;
                        }
                        if (ck == ChangeKindWithDependency.None)
                        {
                            Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}: Check incremental...");
                            if (processor.BuildSteps.Cast<ISupportIncrementalBuildStep>().All(step => step.CanIncrementalBuild(file)))
                            {
                                Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}: Skip build by incremental.");
                                return null;
                            }
                            Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}: Incremental not available.");
                        }
                    }
                }
                return base.Load(processor, metadata, fileMetadata, file);
            }
        }

        private void PostCreate(HostService hostService, IEnumerable<FileAndType> files)
        {
            using (new LoggerPhaseScope("ReportModelLoadInfo", LogLevel.Diagnostic))
            {
                if (!hostService.ShouldTraceIncrementalInfo)
                {
                    return;
                }
                var allFiles = files?.Select(f => f.File) ?? new string[0];
                var loadedFiles = hostService.Models.Select(m => m.FileAndType.File);
                IncrementalContext.ReportModelLoadInfo(hostService, allFiles.Except(loadedFiles), null);
                IncrementalContext.ReportModelLoadInfo(hostService, loadedFiles, BuildPhase.Compile);
            }
        }

        private bool ShouldProcessorTraceInfoCore(IDocumentProcessor processor)
        {
            return IncrementalContext.ShouldProcessorTraceInfo(processor);
        }

        private bool CanProcessorIncrementalCore(IDocumentProcessor processor)
        {
            if (!ShouldProcessorTraceInfo(processor))
            {
                return false;
            }
            return IncrementalContext.CanProcessorIncremental(processor);
        }
    }
}
