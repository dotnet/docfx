// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class TreeIterator
    {
        public static async Task PreorderAsync<T>(T current, T parent, Func<T, IEnumerable<T>> childrenGetter, Func<T, T, Task<bool>> action)
        {
            if (current == null || action == null)
            {
                return;
            }

            if (!await action(current, parent))
            {
                return;
            }

            if (childrenGetter == null)
            {
                return;
            }

            var children = childrenGetter(current);
            if (children != null)
            {
                foreach (var child in children)
                {
                    await PreorderAsync(child, current, childrenGetter, action);
                }
            }
        }

        public static void Preorder<T>(T current, T parent, Func<T, IEnumerable<T>> childrenGetter, Func<T, T, bool> action)
        {
            if (current == null || action == null)
            {
                return;
            }

            if (!action(current, parent))
            {
                return;
            }

            if (childrenGetter == null)
            {
                return;
            }

            var children = childrenGetter(current);
            if (children != null)
            {
                foreach (var child in children)
                {
                    Preorder(child, current, childrenGetter, action);
                }
            }
        }

        public static T PreorderFirstOrDefault<T>(T current, Func<T, IEnumerable<T>> childrenGetter, Func<T, bool> predicate)
        {
            if (predicate(current)) return current;
            if (childrenGetter == null) return default(T);
            var children = childrenGetter(current);
            if (children == null) return default(T);
            foreach(var child in children)
            {
                var result = PreorderFirstOrDefault(child, childrenGetter, predicate);
                if (!object.Equals(result, default(T)))
                {
                    return result;
                }
            }

            return default(T);
        }
    }

    public static class YamlViewModelExtension
    {
        public static bool IsPageLevel(this MemberType type)
        {
            return type == MemberType.Namespace || type == MemberType.Class || type == MemberType.Enum || type == MemberType.Delegate || type == MemberType.Interface || type == MemberType.Struct;
        }

        /// <summary>
        /// Allow multiple items in one yml file
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool AllowMultipleItems(this MemberType type)
        {
            return type == MemberType.Class || type == MemberType.Enum || type == MemberType.Delegate || type == MemberType.Interface || type == MemberType.Struct;
        }

        public static MetadataItem Shrink(this MetadataItem item)
        {
            MetadataItem shrinkedItem = new MetadataItem();
            shrinkedItem.Name = item.Name;

            shrinkedItem.Summary = item.Summary;
            shrinkedItem.Type = item.Type;
            shrinkedItem.Href = item.Href;
            return shrinkedItem;
        }
        public static MetadataItem ShrinkToSimpleToc(this MetadataItem item)
        {
            MetadataItem shrinkedItem = new MetadataItem();
            shrinkedItem.Name = item.Name;
            shrinkedItem.DisplayNames = item.DisplayNames;

            shrinkedItem.Href = item.Href;
            shrinkedItem.Items = null;

            if (item.Items == null)
            {
                return shrinkedItem;
            }

            if (item.Type == MemberType.Toc || item.Type == MemberType.Namespace)
            {
                foreach (var i in item.Items)
                {
                    if (shrinkedItem.Items == null)
                    {
                        shrinkedItem.Items = new List<MetadataItem>();
                    }

                    if (i.IsInvalid) continue;
                    var shrinkedI = i.ShrinkToSimpleToc();
                    shrinkedItem.Items.Add(shrinkedI);
                }

            }

            return shrinkedItem;
        }

        /// <summary>
        /// Only when Namespace is not empty, return it
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static MetadataItem ShrinkToSimpleTocWithNamespaceNotEmpty(this MetadataItem item)
        {
            MetadataItem shrinkedItem = new MetadataItem();
            shrinkedItem.Name = item.Name;
            shrinkedItem.DisplayNames = item.DisplayNames;
            shrinkedItem.Type = item.Type;
            shrinkedItem.Href = item.Href;
            shrinkedItem.Items = null;

            if (item.Type == MemberType.Toc || item.Type == MemberType.Namespace)
            {
                if (item.Items != null)
                {
                    foreach (var i in item.Items)
                    {
                        if (shrinkedItem.Items == null)
                        {
                            shrinkedItem.Items = new List<MetadataItem>();
                        }

                        if (i.IsInvalid) continue;
                        var shrinkedI = i.ShrinkToSimpleTocWithNamespaceNotEmpty();
                        if (shrinkedI != null) shrinkedItem.Items.Add(shrinkedI);
                    }
                }
            }

            if (item.Type == MemberType.Namespace)
            {
                if (shrinkedItem.Items == null || shrinkedItem.Items.Count == 0) return null;
            }

            return shrinkedItem;
        }
    }
}
