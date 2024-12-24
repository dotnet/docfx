// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Build.Engine;
using Docfx.Build.ManagedReference;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.MarkdigEngine;
using Docfx.Plugins;

namespace Docfx;

internal class MetadataMerger
{
    private readonly Dictionary<string, Dictionary<string, object>> _metaTable = [];
    private readonly Dictionary<string, Dictionary<string, object>> _propTable = [];

    public void Merge(MetadataMergeParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.OutputBaseDir == null)
        {
            throw new ArgumentException("Output folder cannot be null.", nameof(parameters) + "." + nameof(parameters.OutputBaseDir));
        }
        if (parameters.Files == null)
        {
            throw new ArgumentException("Source files cannot be null.", nameof(parameters) + "." + nameof(parameters.Files));
        }
        parameters.Metadata ??= ImmutableDictionary<string, object>.Empty;

        Directory.CreateDirectory(parameters.OutputBaseDir);
        Logger.LogInfo("Start merge metadata...");
        MergePageViewModel(parameters);
        MergeToc(parameters);
        Logger.LogInfo("Merge metadata completed.");
    }

    private void MergePageViewModel(MetadataMergeParameters parameters)
    {
        var p = new ManagedReferenceDocumentProcessor
        {
            BuildSteps = new List<IDocumentBuildStep>
            {
                new ApplyPlatformVersion(),
                new MergeManagedReferenceDocument(),
            }
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
            new MarkdigMarkdownService(
                new MarkdownServiceParameters
                {
                    BasePath = fc.DefaultBaseDir,
                }));
        foreach (var m in models)
        {
            m.File = (RelativePath)m.FileAndType.DestinationDir + ((RelativePath)m.File - (RelativePath)m.FileAndType.SourceDir);
            Console.WriteLine($"File:{m.OriginalFileAndType.File} from:{m.FileAndType.SourceDir} to:{m.FileAndType.DestinationDir} => {m.File}");
        }
        foreach (var m in models)
        {
            InitTable(m, parameters.TocMetadata);
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
            select YamlUtility.Deserialize<List<TocItemViewModel>>(f.File));
        CopyMetadataToToc(vm);
        YamlUtility.Serialize(
            ((RelativePath)tocFiles[0].DestinationDir + ((RelativePath)tocFiles[0].File - (RelativePath)tocFiles[0].SourceDir)).ToString(),
            vm,
            YamlMime.TableOfContent);
    }

    private void InitTable(FileModel model, ImmutableList<string> metaNames)
    {
        var content = model.Content as PageViewModel;
        if (content?.Items != null)
        {
            var items = from item in content.Items
                        select YamlUtility.ConvertTo<Dictionary<string, object>>(item);

            foreach (var item in items)
            {
                var property = GetTableItem(item, metaNames);

                if (property.Count > 0 && item.TryGetValue("uid", out object uid))
                {
                    _propTable.Add((string)uid, property);
                }
            }
        }

        if (content?.Metadata != null && model.Uids.Length > 0)
        {
            var metadata = GetTableItem(content.Metadata, metaNames);

            if (metadata.Count > 0)
            {
                // share metadata for all uid in model
                foreach (var uid in model.Uids)
                {
                    _metaTable.Add(uid.Name, metadata);
                }
            }
        }
    }

    private static Dictionary<string, object> GetTableItem(IReadOnlyDictionary<string, object> metadata, ImmutableList<string> metaNames)
    {
        var tableItem = new Dictionary<string, object>();
        foreach (var metaName in metaNames)
        {
            if (metadata.TryGetValue(metaName, out object metaValue))
            {
                tableItem.Add(metaName, metaValue);
            }
        }
        return tableItem;
    }

    private void CopyMetadataToToc(List<TocItemViewModel> vm)
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
        ApplyTocMetadata(item, _metaTable);
        ApplyTocMetadata(item, _propTable);
    }

    private static void ApplyTocMetadata(TocItemViewModel item, Dictionary<string, Dictionary<string, object>> table)
    {
        if (table.TryGetValue(item.Uid, out Dictionary<string, object> metadata))
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

    private static List<TocItemViewModel> MergeTocViewModel(IEnumerable<List<TocItemViewModel>> items)
    {
        return new(
            (from item in items
             where item.Count > 0
             select item).ToList()
            .Merge(TocComparer.Instance, MergeTocItem)
            .OrderBy(x => x.Name));
    }

    private sealed class TocComparer
        : Comparer<TocItemViewModel>
    {
        public static readonly TocComparer Instance = new();

        public override int Compare(TocItemViewModel x, TocItemViewModel y)
        {
            return string.Compare(x.Uid, y.Uid);
        }
    }
}
