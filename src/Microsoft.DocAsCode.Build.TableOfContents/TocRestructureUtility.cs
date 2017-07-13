// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    internal static class TocRestructureUtility
    {
        public static void Restructure(TocItemViewModel toc, IList<TreeItemRestructure> restructures)
        {
            if (restructures == null || restructures.Count == 0)
            {
                return;
            }
            RestructureCore(toc, new List<TocItemViewModel>(), restructures);
        }

        private static void RestructureCore(TocItemViewModel item, List<TocItemViewModel> items, IList<TreeItemRestructure> restructures)
        {
            foreach (var restruction in restructures)
            {
                if (Matches(item, restruction))
                {
                    RestructureItem(item, items, restruction);
                }
            }

            if (item.Items != null && item.Items.Count > 0)
            {
                var parentItems = new List<TocItemViewModel>(item.Items);
                foreach (var i in item.Items)
                {
                    RestructureCore(i, parentItems, restructures);
                }

                item.Items = new TocViewModel(parentItems);
            }
        }

        private static bool Matches(TocItemViewModel item, TreeItemRestructure restruction)
        {
            switch (restruction.TypeOfKey)
            {
                case TreeItemKeyType.TopicUid:
                    // make sure TocHref is null so that TopicUid is not the resolved homepage in `href: api/` case
                    return item.TocHref == null && item.TopicUid == restruction.Key;
                case TreeItemKeyType.TopicHref:
                    return item.TocHref == null && FilePathComparer.OSPlatformSensitiveStringComparer.Compare(item.TopicHref, restruction.Key) == 0;
                default:
                    throw new NotSupportedException($"{restruction.TypeOfKey} is not a supported ComparerKeyType");
            }
        }

        private static void RestructureItem(TocItemViewModel item, List<TocItemViewModel> items, TreeItemRestructure restruction)
        {
            var index = items.IndexOf(item);
            if (index < 0)
            {
                Logger.LogWarning($"Unable to find {restruction.Key}, it is probably removed or replaced by other restructions.");
                return;
            }

            switch (restruction.ActionType)
            {
                case TreeItemActionType.ReplaceSelf:
                    {
                        if (restruction.RestructuredItems == null || restruction.RestructuredItems.Count == 0)
                        {
                            return;
                        }
                        if (restruction.RestructuredItems.Count > 1)
                        {
                            throw new InvalidOperationException($"{restruction.ActionType} does not allow multiple root nodes.");
                        }

                        var roots = GetRoots(restruction.RestructuredItems);
                        items[index] = roots[0];
                        break;
                    }
                case TreeItemActionType.DeleteSelf:
                    {
                        items.RemoveAt(index);
                        break;
                    }
                case TreeItemActionType.AppendChild:
                    {
                        if (restruction.RestructuredItems == null || restruction.RestructuredItems.Count == 0)
                        {
                            return;
                        }
                        if (item.Items == null)
                        {
                            item.Items = new TocViewModel();
                        }

                        var roots = GetRoots(restruction.RestructuredItems);
                        item.Items.AddRange(roots);
                        break;
                    }
                case TreeItemActionType.PrependChild:
                    {
                        if (restruction.RestructuredItems == null || restruction.RestructuredItems.Count == 0)
                        {
                            return;
                        }
                        if (item.Items == null)
                        {
                            item.Items = new TocViewModel();
                        }

                        var roots = GetRoots(restruction.RestructuredItems);
                        item.Items.InsertRange(0, roots);
                        break;
                    }
                case TreeItemActionType.InsertAfter:
                    {
                        if (restruction.RestructuredItems == null || restruction.RestructuredItems.Count == 0)
                        {
                            return;
                        }
                        var roots = GetRoots(restruction.RestructuredItems);
                        items.InsertRange(index + 1, roots);
                        break;
                    }
                case TreeItemActionType.InsertBefore:
                    {
                        if (restruction.RestructuredItems == null || restruction.RestructuredItems.Count == 0)
                        {
                            return;
                        }
                        var roots = GetRoots(restruction.RestructuredItems);
                        items.InsertRange(index, roots);
                        break;
                    }
                default:
                    break;
            }
        }

        private static List<TocItemViewModel> GetRoots(IEnumerable<TreeItem> treeItems)
        {
            return JsonUtility.FromJsonString<List<TocItemViewModel>>(JsonUtility.ToJsonString(treeItems));
        }
    }
}
