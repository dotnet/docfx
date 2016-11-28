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
    using Microsoft.DocAsCode.Utility;

    using TypeForwardedToRelativePath = Microsoft.DocAsCode.Common.RelativePath;

    public class MetadataMerger
    {
        public const string PhaseName = "Merge Metadata";

        private readonly Dictionary<string, HashSet<string>> _tagsRecord = new Dictionary<string, HashSet<string>>();

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

        private void MergePageViewModel(MetadataMergeParameters parameters, string outputBase)
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
                m.File = (TypeForwardedToRelativePath)m.FileAndType.DestinationDir + (((TypeForwardedToRelativePath)m.File) - (TypeForwardedToRelativePath)m.FileAndType.SourceDir);
                Console.WriteLine($"File:{m.OriginalFileAndType.File} from:{m.FileAndType.SourceDir} to:{m.FileAndType.DestinationDir} => {m.File}");
            }
            foreach (var m in models)
            {
                InitializeTagsRecord(m);
                YamlUtility.Serialize(Path.Combine(outputBase, m.File), m.Content, YamlMime.ManagedReference);
            }
        }

        private void MergeToc(MetadataMergeParameters parameters, string outputBase)
        {
            var tocFiles =
                (from f in parameters.Files.EnumerateFiles()
                 where f.Type == DocumentType.Article && "toc.yml".Equals(Path.GetFileName(f.File), StringComparison.OrdinalIgnoreCase)
                 select f).ToList();
            var vm = MergeTocViewModel(
                from f in tocFiles
                select YamlUtility.Deserialize<TocViewModel>(Path.Combine(f.BaseDir, f.File)));
            SetTocViewModelTags(vm);
            YamlUtility.Serialize(
                Path.Combine(
                    outputBase,
                    (TypeForwardedToRelativePath)tocFiles[0].DestinationDir + (((TypeForwardedToRelativePath)tocFiles[0].File) - (TypeForwardedToRelativePath)tocFiles[0].SourceDir)),
                vm,
                YamlMime.TableOfContent);
        }

        private void InitializeTagsRecord(FileModel model)
        {
            var content = model.Content as PageViewModel;
            if (content != null)
            {
                var metadata = content.Metadata;
                object tagsObj;
                if (metadata != null && metadata.TryGetValue("tags", out tagsObj))
                {
                    var tags = tagsObj as object[];
                    if (tags != null)
                    {
                        var tagsList = tags.Select(tag => tag as string).Where(tag => tag != null).ToArray();
                        _tagsRecord.Add(model.Uids.FirstOrDefault().Name, new HashSet<string>(tagsList));
                    }
                    else
                    {
                        Logger.LogWarning("fileMetadata tags should be an array of string.");
                    }
                }
            }
        }

        private void SetTocViewModelTags(TocViewModel vm)
        {
            foreach (var item in vm)
            {
                MergeTags(item);
            }
        }

        private HashSet<string> MergeTags(TocItemViewModel item)
        {
            HashSet<string> tags;
            if (!_tagsRecord.TryGetValue(item.Uid, out tags))
            {
                tags = new HashSet<string>();
            }
            foreach (var child in item.Items ?? Enumerable.Empty<TocItemViewModel>())
            {
                var childTags = MergeTags(child);
                tags.UnionWith(childTags);
            }
            item.Tags = tags;
            return tags;
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
