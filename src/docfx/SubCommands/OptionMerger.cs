// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    internal static class OptionMerger
    {
        public delegate T Merger<T>(string key, MergeContext<T> item, MergeContext<T> overrideItem);

        public static Dictionary<string, T> MergeDictionary<T>(DictionaryMergeContext<T> item, DictionaryMergeContext<T> overrideItem, Merger<T> merger)
        {
            Dictionary<string, T> merged;
            if (overrideItem?.Item == null)
            {
                merged = new Dictionary<string, T>();
            }
            else
            {
                merged = new Dictionary<string, T>(overrideItem.Item);
            }
            if (item?.Item == null)
            {
                return merged;
            }
            else
            {
                foreach (var pair in item.Item)
                {
                    T value;
                    if (merged.TryGetValue(pair.Key, out value))
                    {
                        Logger.LogWarning($"Both {item.Name} and {overrideItem.Name} contain definition for \"{pair.Key}\", the one from \"{overrideItem.Name}\" overrides the one from \"{item.Name}\".");
                        merged[pair.Key] = merger(pair.Key, new MergeContext<T>(item.Name, pair.Value), new MergeContext<T>(overrideItem.Name, value));
                    }
                    else
                    {
                        merged[pair.Key] = pair.Value;
                    }
                }
            }
            return merged;
        }
    }

    internal sealed class DictionaryMergeContext<T>
    {
        public string Name { get; }
        public Dictionary<string, T> Item { get; }

        public DictionaryMergeContext(string name, Dictionary<string, T> item)
        {
            Name = name;
            Item = item;
        }
    }

    internal sealed class MergeContext<T>
    {
        public string Name { get; }
        public T Item { get; }

        public MergeContext(string name, T item)
        {
            Name = name;
            Item = item;
        }
    }
}
