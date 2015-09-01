// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;
    using System.Collections.Generic;

    public class ConverterModel : Dictionary<FileAndType, FileModel>
    {
        public ConverterModel(string baseDir)
        {
            BaseDir = baseDir;
        }

        public string BaseDir { get; private set; }

        public ConverterModel AddRange(ConverterModel r)
        {
            if (BaseDir != r.BaseDir)
            {
                throw new ArgumentException("r");
            }
            foreach (var item in r)
            {
                this[item.Key] = item.Value;
            }
            return this;
        }
    }

    public interface IHasUidIndex
    {
        Dictionary<string, HashSet<FileAndType>> UidIndex { get; set; }

        Dictionary<string, UidTreeNode> UidTree { get; set; }
    }

    public class UidTreeNode
    {
        public UidTreeNode(string uid, string parent)
        {
            Uid = uid;
            Parent = parent;
            Children = new HashSet<string>();
        }

        public string Uid { get; private set; }

        public string Parent { get; private set; }

        public HashSet<string> Children { get; private set; }

        public bool? IsPage { get; set; }
    }
}
