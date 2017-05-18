// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;

    public abstract class XRefRedirectionReader : IXRefContainerReader
    {
        private readonly string _majorName;
        private readonly HashSet<string> _mapNames;

        protected XRefRedirectionReader(string majorName, HashSet<string> mapNames)
        {
            _majorName = majorName ?? throw new ArgumentNullException(nameof(majorName));
            _mapNames = mapNames ?? throw new ArgumentNullException(nameof(mapNames));
            if (!mapNames.Contains(majorName))
            {
                throw new ArgumentException("Major map not found.");
            }
        }

        protected abstract IXRefContainer GetMap(string name);

        public XRefSpec Find(string uid)
        {
            var searched = new HashSet<string>();
            var checkList = new Stack<string>();
            checkList.Push(_majorName);

            while (checkList.Count > 0)
            {
                var currentKey = checkList.Pop();
                if (searched.Contains(currentKey))
                {
                    continue;
                }

                var currentMap = GetMap(currentKey);
                if (currentMap == null)
                {
                    continue;
                }

                var result = currentMap.GetReader().Find(uid);
                if (result != null)
                {
                    return result;
                }

                searched.Add(currentKey);
                AddRedirections(uid, checkList, currentMap);
            }
            return null;
        }

        private void AddRedirections(string uid, Stack<string> checkList, IXRefContainer current)
        {
            foreach (var r in current.GetRedirections().Reverse())
            {
                if (r.UidPrefix == null ||
                    uid.StartsWith(r.UidPrefix))
                {
                    if (r.Href != null)
                    {
                        checkList.Push(r.Href);
                    }
                }
            }
        }
    }
}
