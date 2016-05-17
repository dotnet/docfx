// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Plugins;

    public abstract class XRefRedirectionReader : IXRefContainerReader
    {
        private string _majorName;
        private readonly HashSet<string> _mapNames;

        protected XRefRedirectionReader(string majorName, HashSet<string> mapNames)
        {
            if (majorName == null)
            {
                throw new ArgumentNullException(nameof(majorName));
            }
            if (mapNames == null)
            {
                throw new ArgumentNullException(nameof(mapNames));
            }
            if (!mapNames.Contains(majorName))
            {
                throw new ArgumentException("Major map not found.");
            }
            _majorName = majorName;
            _mapNames = mapNames;
        }

        protected abstract XRefMap GetMap(string name);

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

                var result = new BasicXRefMapReader(currentMap).Find(uid);
                if (result != null)
                {
                    return result;
                }

                searched.Add(currentKey);
                if (currentMap.Redirections != null)
                {
                    AddRedirections(uid, checkList, currentMap);
                }
            }
            return null;
        }

        private void AddRedirections(string uid, Stack<string> checkList, XRefMap current)
        {
            for (int i = current.Redirections.Count - 1; i >= 0; i--)
            {
                var r = current.Redirections[i];
                if (r.UidPrefix != null &&
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
