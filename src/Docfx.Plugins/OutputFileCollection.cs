// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Plugins;

public class OutputFileCollection : ObservableDictionary<string, OutputFileInfo>
{
    #region Binary Compatibility

    public new OutputFileInfo this[string key]
    {
        get { return base[key]; }
        set { base[key] = value; }
    }

    public new IEqualityComparer<string> Comparer => base.Comparer;

    public new int Count => base.Count;

    public new ICollection<string> Keys => base.Keys;

    public new ICollection<OutputFileInfo> Values => base.Values;

    public new void Add(string key, OutputFileInfo value) => base.Add(key, value);

    public new void Clear() => base.Clear();

    public new bool ContainsKey(string key) => base.ContainsKey(key);

    public new IEnumerator<KeyValuePair<string, OutputFileInfo>> GetEnumerator() => base.GetEnumerator();

    public new bool Remove(string key) => base.Remove(key);

    public new bool TryGetValue(string key, out OutputFileInfo value) => base.TryGetValue(key, out value);

    #endregion

    public bool ContainsValue(OutputFileInfo value) => ContainsValue(value);
}
