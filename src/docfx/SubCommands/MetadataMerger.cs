// Copyright (c) Microsoft. All rights reserved.
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
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    public class MetadataMerger
    {
        public const string PhaseName = "Merge Metadata";

        private readonly Dictionary<string, Dictionary<string, object>> _metaTable = new Dictionary<string, Dictionary<string, object>>();

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
                MergePageViewModel(parameters);
                MergeToc(parameters);
                Logger.LogInfo("Merge metadata completed.");
            }
        }

        private void MergePageViewModel(MetadataMergeParameters parameters)
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
                InitMetaTable(m, parameters.TocMetadata);
                YamlUtility.Serialize(m.File, m.Content, YamlMime.ManagedReference);
            }
        }

        private void MergeToc(MetadataMergeParameters parameters)
        {
            var tocFiles =
                (from f in parameters.Files.EnumerateFiles()
                 where f.Type == DocumentType.Article && "toc.yml".Equals(Path.GetFileName(f.File), StringComparison.OrdinalIgnoreCase)
                 select f).ToList();
            var vm = MergeTocViewModel(
                from f in tocFiles
                select YamlUtility.Deserialize<TocViewModel>(f.File));
            CopyMetadataToToc(vm);
            YamlUtility.Serialize(
                ((RelativePath)tocFiles[0].DestinationDir + (((RelativePath)tocFiles[0].File) - (RelativePath)tocFiles[0].SourceDir)).ToString(),
                vm,
                YamlMime.TableOfContent);
        }

        private void InitMetaTable(FileModel model, ImmutableList<string> metaNames)
        {
            var content = model.Content as PageViewModel;
            if (content?.Metadata != null && model.Uids.Length > 0)
            {
                var metadata = new Dictionary<string, object>();
                foreach (var metaName in metaNames)
                {
                    object metaValue;
                    if (content.Metadata.TryGetValue(metaName, out metaValue))
                    {
                        metadata.Add(metaName, metaValue);
                    }
                }
                if (metadata.Count > 0)
                {
                    _metaTable.Add(model.Uids.First().Name, metadata);
                }
            }
        }

        private void CopyMetadataToToc(TocViewModel vm)
        {
            foreach (var item in vm)
            {
                CopyMetadataToTocItem(item);
                foreach (var childItem in item.Items ?? Enumerable.Empty<TocItemViewModel>())
                {
                    CopyMetadataToTocItem(childItem);
                }
            }
        }

        private void CopyMetadataToTocItem(TocItemViewModel item)
        {
            Dictionary<string, object> metadata;
            if (_metaTable.TryGetValue(item.Uid, out metadata))
            {
                foreach (var metaPair in metadata)
                {
                    item.Metadata[metaPair.Key] = metaPair.Value;
                }
            }
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
