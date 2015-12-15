// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;

    public static class YamlViewModelExtensions
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
