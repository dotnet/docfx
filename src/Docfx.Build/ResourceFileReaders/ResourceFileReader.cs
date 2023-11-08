// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Docfx.Build.Engine;

public abstract class ResourceFileReader
{
    public abstract string Name { get; }

    public abstract bool IsEmpty { get; }

    public abstract IEnumerable<string> Names { get; }

    public virtual string GetResource(string name)
    {
        using var stream = GetResourceStream(name);
        return GetString(stream);
    }

    public IEnumerable<ResourceInfo> GetResources(string selector = null)
    {
        foreach (var pair in GetResourceStreams(selector))
        {
            using (pair.Value)
            {
                yield return new ResourceInfo(pair.Key, GetString(pair.Value));
            }
        }
    }

    public IEnumerable<KeyValuePair<string, Stream>> GetResourceStreams(string selector = null)
    {
        bool filter(string s)
        {
            if (selector != null)
            {
                var regex = new Regex(selector, RegexOptions.IgnoreCase);
                return regex.IsMatch(s);
            }
            else
            {
                return true;
            }
        }
        foreach (var name in Names)
        {
            if (filter(name))
            {
                yield return new KeyValuePair<string, Stream>(name, GetResourceStream(name));
            }
        }
    }

    public abstract Stream GetResourceStream(string name);

    protected static string GetString(Stream stream)
    {
        if (stream == null) return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
