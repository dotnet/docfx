﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Build.ManagedReference;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class MetadataMerger
    {
        public const string PhaseName = "Merge Metadata";

        public void Merge(MetadataMergeParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (parameters.OutputBaseDir == null)
            {
                throw new ArgumentException("Output folder cannot be null.", nameof(parameters) + "." + nameof(parameters.OutputBaseDir));
            }
            if (parameters.Files == null)
            {
                throw new ArgumentException("Source files cannot be null.", nameof(parameters) + "." + nameof(parameters.Files));
            }
            if (parameters.Metadata == null)
            {
                parameters.Metadata = ImmutableDictionary<string, object>.Empty;
            }

            using (new LoggerPhaseScope(PhaseName))
            {
                Directory.CreateDirectory(parameters.OutputBaseDir);
                Logger.LogInfo("Start merge metadata...");
                MergePageViewModel(parameters, Directory.GetCurrentDirectory());
                MergeToc(parameters, Directory.GetCurrentDirectory());
                Logger.LogInfo("Merge metadata completed.");
            }
        }

        private static void MergePageViewModel(MetadataMergeParameters parameters, string outputBase)
        {
            var p = new ManagedReferenceDocumentProcessor();
            p.BuildSteps = new List<IDocumentBuildStep>
            {
                new ApplyPlatformVersion(),
                new MergeManagedReferenceDocument(),
            };
            var fc = new FileCollection(parameters.Files);
            fc.RemoveAll(x => "toc.yml".Equals(Path.GetFileName(x.File), StringComparison.OrdinalIgnoreCase));
            var models = SingleDocumentBuilder.Build(
                p,
                new DocumentBuildParameters
                {
                    Files = fc,
                    FileMetadata = parameters.FileMetadata,
                    MaxParallelism = 1,
                    Metadata = parameters.Metadata,
                    OutputBaseDir = parameters.OutputBaseDir,
                },
                new DfmServiceProvider().CreateMarkdownService(
                    new MarkdownServiceParameters
                    {
                        BasePath = fc.DefaultBaseDir,
                    }));
            foreach (var m in models)
            {
                m.File = (RelativePath)m.FileAndType.DestinationDir + (((RelativePath)m.File) - (RelativePath)m.FileAndType.SourceDir);
                Console.WriteLine($"File:{m.OriginalFileAndType.File} from:{m.FileAndType.SourceDir} to:{m.FileAndType.DestinationDir} => {m.File}");
            }
            foreach (var m in models)
            {
                YamlUtility.Serialize(Path.Combine(outputBase, m.File), m.Content, YamlMime.ManagedReference);
            }
        }

        private static void MergeToc(MetadataMergeParameters parameters, string outputBase)
        {
            var tocFiles =
                (from f in parameters.Files.EnumerateFiles()
                 where f.Type == DocumentType.Article && "toc.yml".Equals(Path.GetFileName(f.File), StringComparison.OrdinalIgnoreCase)
                 select f).ToList();
            var vm = MergeTocViewModel(
                from f in tocFiles
                select YamlUtility.Deserialize<TocViewModel>(Path.Combine(f.BaseDir, f.File)));
            YamlUtility.Serialize(
                Path.Combine(
                    outputBase,
                    (RelativePath)tocFiles[0].DestinationDir + (((RelativePath)tocFiles[0].File) - (RelativePath)tocFiles[0].SourceDir)),
                vm,
                YamlMime.TableOfContent);
        }

        private static TocItemViewModel MergeTocItem(List<TocItemViewModel> items)
        {
            var first = items[0];
            if (items.Count > 1)
            {
                first.Items = MergeTocViewModel(
                    from item in items
                    where item.Items?.Count > 0
                    select item.Items);
                if (first.Items.Count == 0)
                {
                    first.Items = null;
                }
            }
            return first;
        }

        private static TocViewModel MergeTocViewModel(IEnumerable<TocViewModel> items)
        {
            return new TocViewModel(
                (from item in items
                 where item.Count > 0
                 select item).ToList()
                .Merge(TocComparer.Instance, MergeTocItem)
                .OrderBy(x => x.Name));
        }

        private sealed class TocComparer
            : Comparer<TocItemViewModel>
        {
            public static readonly TocComparer Instance = new TocComparer();

            public override int Compare(TocItemViewModel x, TocItemViewModel y)
            {
                return string.Compare(x.Uid, y.Uid);
            }
        }
    }
}
