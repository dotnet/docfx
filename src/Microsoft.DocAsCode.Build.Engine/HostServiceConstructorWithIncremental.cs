// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class HostServiceConstructorWithIncremental : HostServiceConstructor, IHostServiceConstructor
    {
        public IncrementalBuildContext IncrementalContext { get; }

        public HostServiceConstructorWithIncremental(DocumentBuildContext context) : base(context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            IncrementalContext = context.IncrementalBuildContext;
        }

        public override bool ShouldProcessorTraceInfo(IDocumentProcessor processor)
        {
            if (!(processor is ISupportIncrementalDocumentProcessor))
            {
                Logger.LogVerbose($"Processor {processor.Name} cannot suppport incremental build because the processor doesn't implement {nameof(ISupportIncrementalDocumentProcessor)} interface.");
                return false;
            }
            if (!processor.BuildSteps.All(step => step is ISupportIncrementalBuildStep))
            {
                Logger.LogVerbose($"Processor {processor.Name} cannot suppport incremental build because the following steps don't implement {nameof(ISupportIncrementalBuildStep)} interface: {string.Join(",", processor.BuildSteps.Where(step => !(step is ISupportIncrementalBuildStep)).Select(s => s.Name))}.");
                return false;
            }
            return true;
        }

        public override bool CanProcessorIncremental(IDocumentProcessor processor)
        {
            if (!ShouldProcessorTraceInfo(processor))
            {
                return false;
            }
            IncrementalContext.CreateProcessorInfo(processor);
            return IncrementalContext.CanProcessorIncremental(processor);
        }

        public override void PostConstruct(HostService hostService, IEnumerable<FileAndType> files)
        {
            base.PostConstruct(hostService, files);
            using (new LoggerPhaseScope("ReportModelLoadInfo", true))
            {
                var allFiles = files?.Select(f => f.File) ?? new string[0];
                var loadedFiles = hostService.Models.Select(m => m.FileAndType.File);
                IncrementalContext.ReportModelLoadInfo(hostService, allFiles.Except(loadedFiles), null);
                IncrementalContext.ReportModelLoadInfo(hostService, loadedFiles, BuildPhase.PreBuild);
            }
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
    }
}
