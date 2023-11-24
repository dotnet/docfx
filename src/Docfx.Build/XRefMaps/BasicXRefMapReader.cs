// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Build.Engine;

public class BasicXRefMapReader : IXRefContainerReader
{
    protected XRefMap Map { get; }

    public BasicXRefMapReader(XRefMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        Map = map;
        if (map.HrefUpdated != true &&
            map.BaseUrl != null)
        {
            if (!Uri.TryCreate(map.BaseUrl, UriKind.Absolute, out Uri baseUri))
            {
                throw new InvalidDataException($"Xref map file has an invalid base url: {map.BaseUrl}.");
            }
            map.UpdateHref(baseUri);
        }
    }

    public virtual XRefSpec Find(string uid)
    {
        if (Map.References == null)
        {
            return null;
        }
        if (Map.Sorted == true)
        {
            var index = Map.References.BinarySearch(new XRefSpec { Uid = uid }, XRefSpecUidComparer.Instance);
            if (index >= 0)
            {
                return Map.References[index];
            }
            return null;
        }
        else
        {
            return Map.References.Find(x => x.Uid == uid);
        }
    }
}
