// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Build.Engine;

public abstract class XRefRedirectionReader : IXRefContainerReader
{
    private readonly string _majorName;

    protected XRefRedirectionReader(string majorName, HashSet<string> mapNames)
    {
        ArgumentNullException.ThrowIfNull(majorName);

        _majorName = majorName;
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

    private static void AddRedirections(string uid, Stack<string> checkList, IXRefContainer current)
    {
        foreach (var r in current.GetRedirections().Reverse())
        {
            if (r.UidPrefix == null ||
                uid.StartsWith(r.UidPrefix, StringComparison.Ordinal))
            {
                if (r.Href != null)
                {
                    checkList.Push(r.Href);
                }
            }
        }
    }
}
