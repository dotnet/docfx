// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class FileModel
    {
        public FileModel(FileAndType ft, Dictionary<object, object> content)
        {
            FileAndType = ft;
            Content = content;
        }

        public FileAndType FileAndType { get; private set; }

        public Dictionary<object, object> Content { get; private set; }

        public string BaseDir => FileAndType.BaseDir;

        public string File => FileAndType.File;

        public DocumentType Type => FileAndType.Type;

        public IEnumerable<Dictionary<object, object>> GetItems()
        {
            if (Content == null)
            {
                yield break;
            }
            object value;
            Content.TryGetValue("items", out value);
            var items = value as List<object>;
            if (items == null)
            {
                yield break;
            }
            foreach (var item in items)
            {
                var result = item as Dictionary<object, object>;
                if (result!= null)
                {
                    yield return result;
                }
            }
        }

        public Dictionary<object, object> GetItem(string uid)
        {
            foreach (var item in GetItems())
            {
                object value;
                item.TryGetValue("uid", out value);
                var currentUid = value as string;
                if (currentUid == uid)
                {
                    return item;
                }
            }
            return null;
        }

        public IEnumerable<string> GetUids()
        {
            foreach (var item in GetItems())
            {
                object value;
                item.TryGetValue("uid", out value);
                var uid = value as string;
                if (uid != null)
                {
                    yield return uid;
                }
            }
        }

        public IEnumerable<UidRelationship> GetRelationships()
        {
            foreach (var item in GetItems())
            {
                object value;
                item.TryGetValue("uid", out value);
                var uid = value as string;
                if (uid == null)
                {
                    continue;
                }
                var result = new UidRelationship(uid);
                item.TryGetValue("parent", out value);
                result.Parent = value as string;
                item.TryGetValue("children", out value);
                result.Children = (value as IEnumerable<object> ?? new object[0]).OfType<string>().ToList();
                if (item.TryGetValue("isPage", out value))
                {
                    result.IsPage = (bool)Convert.ChangeType(value, typeof(bool));
                }
                yield return result;
            }
        }

        public void UpdateChildren(string uid, List<string> children)
        {
            foreach (var item in GetItems())
            {
                object value;
                item.TryGetValue("uid", out value);
                var currentUid = value as string;
                if (currentUid != uid)
                {
                    continue;
                }
                item["children"] = children;
                return;
            }
            throw new Exception($"Cannot find uid: {uid}");
        }

        public void Replace(string uid, Dictionary<object, object> content)
        {
            if (Content == null)
            {
                return;
            }
            object value;
            Content.TryGetValue("items", out value);
            var items = value as List<object>;
            if (items == null)
            {
                return;
            }
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var result = item as Dictionary<string, object>;
                if (result != null)
                {
                    if (result.TryGetValue("uid", out value))
                    {
                        var currentUid = value as string;
                        if (currentUid != uid)
                        {
                            continue;
                        }
                    }
                    items[i] = content;
                    return;
                }
            }
            throw new Exception($"Cannot find uid: {uid}");
        }
    }
}
