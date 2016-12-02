// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Build.ManagedReference;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    using TypeForwardedToRelativePath = Microsoft.DocAsCode.Common.RelativePath;

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
                InitMetaTable(m);
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
            SetTocViewModelMetadata(vm, parameters.MetadataNeedMergedIntoToc);
            YamlUtility.Serialize(
                Path.Combine(
                    outputBase,
                    (TypeForwardedToRelativePath)tocFiles[0].DestinationDir + (((TypeForwardedToRelativePath)tocFiles[0].File) - (TypeForwardedToRelativePath)tocFiles[0].SourceDir)),
                vm,
                YamlMime.TableOfContent);
        }

        private void InitMetaTable(FileModel model)
        {
            var content = model.Content as PageViewModel;
            if (content != null && model.Uids.Length > 0)
            {
                _metaTable.Add(model.Uids.First().Name, content.Metadata);
            }
        }

        private void SetTocViewModelMetadata(TocViewModel vm, ImmutableList<string> metaNames)
        {
            foreach (var item in vm)
            {
                foreach (var metaName in metaNames)
                {
                    MergeIntoTocMetadata(item, metaName);
                }
            }
        }

        private List<string> MergeIntoTocMetadata(TocItemViewModel item, string metaName)
        {
            Dictionary<string, object> metadata;
            if (_metaTable.TryGetValue(item.Uid, out metadata))
            {
                object metaValue;
                if (metadata.TryGetValue(metaName, out metaValue))
                {
                    var merged = TryGetListFromObject(metaValue);
                    foreach (var child in item.Items ?? Enumerable.Empty<TocItemViewModel>())
                    {
                        var childMetaValue = MergeIntoTocMetadata(child, metaName);
                        merged = MergeMetadata(merged, childMetaValue);
                    }
                    if (item.Metadata == null)
                    {
                        item.Metadata = new Dictionary<string, object>();
                    }
                    item.Metadata[metaName] = merged;
                    return merged;
                }
            }
            return null;
        }

        private static List<string> MergeMetadata(object meta1, object meta2)
        {
            var list1 = TryGetListFromObject(meta1);
            var list2 = TryGetListFromObject(meta2);

            if (list1 == null && list2 == null)
            {
                return null;
            }
            else if (list1 == null)
            {
                return list2.Distinct().ToList();
            }
            else if (list2 == null)
            {
                return list1.Distinct().ToList();
            }

            list1.AddRange(list2);
            return list1.Distinct().ToList();
        }

        private static List<string> TryGetListFromObject(object meta)
        {
            var text = meta as string;
            if (text != null)
            {
                return new List<string> { text };
            }

            var collection = meta as IEnumerable<object>;
            if (collection != null)
            {
                return collection.OfType<string>().ToList();
            }

            var jarray = meta as JArray;
            if (jarray != null)
            {
                try
                {
                    return jarray.ToObject<List<string>>();
                }
                catch (Exception)
                {
                    Logger.LogWarning($"Unknown metadata: {jarray.ToString()}");
                }
            }

            return null;
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
