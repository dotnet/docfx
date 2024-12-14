// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Build.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;

namespace Docfx.Build.ManagedReference;

public class MergeManagedReferenceDocument : BaseDocumentBuildStep
{
    public override int BuildOrder => 0xff;

    public override string Name => nameof(MergeManagedReferenceDocument);

    public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
    {
        host.LogInfo("Merging platform...");
        var processedUid = new HashSet<string>();
        var merged = models.RunAll(m =>
        {
            if (m.Type != DocumentType.Article)
            {
                return m;
            }
            if (m.Uids.Length == 0)
            {
                host.LogWarning("Unknown model without uid.", file: m.File);
                return m;
            }
            var mainUid = m.Uids[0].Name;
            if (processedUid.Contains(mainUid))
            {
                return null;
            }
            var sameTopics = host.LookupByUid(mainUid);
            if (sameTopics.Count == 1)
            {
                return m;
            }
            processedUid.Add(mainUid);
            m.Content = MergeCore(
                mainUid,
                m,
                from topic in sameTopics
                where topic != m
                where topic.Type == DocumentType.Article
                select topic,
                host);
            return m;
        });
        host.LogInfo("Platform merged.");
        return from p in merged
               where p != null
               select p;
    }

    private object MergeCore(string majorUid, FileModel model, IEnumerable<FileModel> others, IHostService host)
    {
        var item = CreateMergeItem(majorUid, model, host);

        if (item == null)
        {
            return model.Content;
        }

        foreach (var other in others)
        {
            var otherItem = CreateMergeItem(majorUid, other, host);
            if (otherItem == null)
            {
                continue;
            }
            MergeCore(item, otherItem);
        }
        return ConvertToVM(item);
    }

    private static MergeItem CreateMergeItem(string majorUid, FileModel model, IHostService host)
    {
        var vm = (PageViewModel)model.Content;
        var majorItem = vm.Items.Find(item => item.Uid == majorUid);
        if (majorItem == null)
        {
            host.LogError("Cannot find uid in model.", file: model.File);
            return null;
        }
        return CreateMergeItemCore(majorItem, vm);
    }

    private static MergeItem CreateMergeItemCore(ItemViewModel majorItem, PageViewModel page)
    {
        return new MergeItem
        {
            MajorItem = majorItem,
            AssemblyNameList = new SortedSet<string>(majorItem.AssemblyNameList ?? Enumerable.Empty<string>()),
            Children = new SortedSet<string>(majorItem.Children ?? Enumerable.Empty<string>()),
            Platform = new SortedSet<string>(majorItem.Platform ?? Enumerable.Empty<string>()),
            MinorItems = page?.Items.Where(x => x.Uid != majorItem.Uid).ToDictionary(item => item.Uid, item => CreateMergeItemCore(item, null)),
            References = page?.References.ToDictionary(item => item.Uid),
            Metadata = page?.Metadata,
        };
    }

    private void MergeCore(MergeItem item, MergeItem otherItem)
    {
        item.AssemblyNameList.UnionWith(otherItem.AssemblyNameList);
        item.Children.UnionWith(otherItem.Children);
        item.Platform.UnionWith(otherItem.Platform);
        MergeMinorItems(item, otherItem);
        MergeReferences(item, otherItem);
    }

    private void MergeMinorItems(MergeItem item, MergeItem otherItem)
    {
        if (item.MinorItems != null)
        {
            if (otherItem.MinorItems != null)
            {
                MergeMinorItemsCore(item.MinorItems, otherItem.MinorItems);
            }
        }
        else if (otherItem.MinorItems != null)
        {
            item.MinorItems = otherItem.MinorItems;
        }
    }

    private void MergeMinorItemsCore(
        Dictionary<string, MergeItem> mergeTo,
        Dictionary<string, MergeItem> mergeFrom)
    {
        foreach (var pair in mergeTo)
        {
            if (mergeFrom.TryGetValue(pair.Key, out MergeItem item))
            {
                MergeCore(pair.Value, item);
                mergeFrom.Remove(pair.Key);
            }
        }
        foreach (var pair in mergeFrom)
        {
            mergeTo[pair.Key] = pair.Value;
        }
    }

    private static void MergeReferences(MergeItem item, MergeItem otherItem)
    {
        if (item.References != null)
        {
            if (otherItem.References != null)
            {
                MergeReferencesCore(item.References, otherItem.References);
            }
        }
        else if (otherItem.References != null)
        {
            item.References = otherItem.References;
        }
    }

    private static void MergeReferencesCore(
        Dictionary<string, ReferenceViewModel> mergeTo,
        Dictionary<string, ReferenceViewModel> mergeFrom)
    {
        foreach (var pair in mergeFrom)
        {
            mergeTo.TryAdd(pair.Key, pair.Value);
        }
    }

    private static PageViewModel ConvertToVM(MergeItem mergeItem)
    {
        var vm = new PageViewModel
        {
            Items = [],
            References = mergeItem.References?.Values.ToList(),
            Metadata = mergeItem.Metadata,
        };
        ConvertToVMCore(vm, mergeItem);
        return vm;
    }

    private static void ConvertToVMCore(PageViewModel vm, MergeItem mergeItem)
    {
        if (mergeItem.AssemblyNameList.Count > 0)
        {
            mergeItem.MajorItem.AssemblyNameList = mergeItem.AssemblyNameList.ToList();
        }
        if (mergeItem.Children.Count > 0)
        {
            mergeItem.MajorItem.Children = mergeItem.Children.ToList();
        }
        if (mergeItem.Platform.Count > 0)
        {
            mergeItem.MajorItem.Platform = mergeItem.Platform.ToList();
        }
        vm.Items.Add(mergeItem.MajorItem);
        if (mergeItem.MinorItems != null)
        {
            foreach (var item in mergeItem.MinorItems.Values)
            {
                ConvertToVMCore(vm, item);
            }
        }
    }

    private sealed class MergeItem
    {
        public ItemViewModel MajorItem { get; set; }
        public SortedSet<string> AssemblyNameList { get; set; }
        public SortedSet<string> Children { get; set; }
        public SortedSet<string> Platform { get; set; }
        public Dictionary<string, MergeItem> MinorItems { get; set; }
        public Dictionary<string, ReferenceViewModel> References { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
