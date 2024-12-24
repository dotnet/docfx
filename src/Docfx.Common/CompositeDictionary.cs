// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Immutable;

namespace Docfx.Common;

public class CompositeDictionary
    : IDictionary<string, object>
{
    private readonly ImmutableArray<Entry> _entries;

    public CompositeDictionary()
    {
        _entries = [];
    }

    private CompositeDictionary(ImmutableArray<Entry> entries)
    {
        _entries = entries;
    }

    public object this[string key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            foreach (var entry in _entries)
            {
                if (key.StartsWith(entry.Prefix, StringComparison.Ordinal))
                {
                    var pair = entry.TryGetValue(key.Substring(entry.Prefix.Length));
                    if (pair.Key)
                    {
                        return pair.Value;
                    }
                    break;
                }
            }
            throw new KeyNotFoundException();
        }
        set
        {
            ArgumentNullException.ThrowIfNull(key);

            foreach (var entry in _entries)
            {
                if (key.StartsWith(entry.Prefix, StringComparison.Ordinal))
                {
                    entry.SetValue(key.Substring(entry.Prefix.Length), value);
                    return;
                }
            }
            throw new InvalidOperationException();
        }
    }

    public int Count => _entries.Sum(e => e.Count());

    public bool IsReadOnly => false;

    public ICollection<string> Keys => (from pair in this select pair.Key).ToList();

    public ICollection<object> Values => (from pair in this select pair.Value).ToList();

    void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
    {
        Add(item.Key, item.Value);
    }

    public void Add(string key, object value)
    {
        ArgumentNullException.ThrowIfNull(key);

        foreach (var entry in _entries)
        {
            if (key.StartsWith(entry.Prefix))
            {
                entry.AddValue(key.Substring(entry.Prefix.Length), value);
                return;
            }
        }
        throw new InvalidOperationException();
    }

    public void Clear()
    {
        foreach (var entry in _entries)
        {
            entry.Clear();
        }
    }

    bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
    {
        return TryGetValue(item.Key, out object value) && object.Equals(item.Value, value);
    }

    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return TryGetValue(key, out _);
    }

    void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        int count = 0;
        foreach (var item in this)
        {
            array[arrayIndex + count] = item;
            count++;
        }
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        foreach (var entry in _entries)
        {
            foreach (var item in entry.Enumerate())
            {
                yield return new KeyValuePair<string, object>(entry.Prefix + item.Key, item.Value);
            }
        }
    }

    bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
    {
        if (!((ICollection<KeyValuePair<string, object>>)this).Contains(item))
        {
            return false;
        }
        return Remove(item.Key);
    }

    public bool Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        foreach (var entry in _entries)
        {
            if (key.StartsWith(entry.Prefix))
            {
                return entry.Remove(key.Substring(entry.Prefix.Length));
            }
        }
        return false;
    }

    public bool TryGetValue(string key, out object value)
    {
        ArgumentNullException.ThrowIfNull(key);

        foreach (var entry in _entries)
        {
            if (key.StartsWith(entry.Prefix))
            {
                var pair = entry.TryGetValue(key.Substring(entry.Prefix.Length));
                value = pair.Value;
                return pair.Key;
            }
        }
        value = null;
        return false;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static Builder CreateBuilder()
    {
        return new Builder();
    }

    private sealed class Entry
    {
        public string Prefix { get; set; }
        public Func<IEnumerable<KeyValuePair<string, object>>> Enumerate { get; set; }
        public Action<string, object> AddValue { get; set; }
        public Action<string, object> SetValue { get; set; }
        public Func<string, KeyValuePair<bool, object>> TryGetValue { get; set; }
        public Func<string, bool> Remove { get; set; }
        public Action Clear { get; set; }
        public Func<int> Count { get; set; }
    }

    public sealed class Builder
    {
        private readonly List<Entry> _entries = [];

        internal Builder() { }

        public Builder Add<TValue>(string prefix, IDictionary<string, TValue> dict, Func<object, TValue> valueConverter = null)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            ArgumentNullException.ThrowIfNull(dict);

            valueConverter ??= o => (TValue)o;
            if (_entries.Exists(e => e.Prefix == prefix))
            {
                throw new InvalidOperationException();
            }
            _entries.Add(new Entry
            {
                Prefix = prefix,
                Enumerate = () => Enumerate(dict),
                AddValue = (k, v) => dict.Add(k, valueConverter(v)),
                SetValue = (k, v) => dict[k] = valueConverter(v),
                TryGetValue =
                    key =>
                    {
                        var getted = dict.TryGetValue(key, out TValue value);
                        return new KeyValuePair<bool, object>(getted, value);
                    },
                Remove = dict.Remove,
                Clear = dict.Clear,
                Count = () => dict.Count,
            });
            return this;
        }

        private static IEnumerable<KeyValuePair<string, object>> Enumerate<TValue>(IEnumerable<KeyValuePair<string, TValue>> source)
        {
            foreach (var item in source)
            {
                yield return new KeyValuePair<string, object>(item.Key, item.Value);
            }
        }

        public CompositeDictionary Create()
        {
            return new CompositeDictionary(_entries.OrderByDescending(x => x.Prefix.Length).ToImmutableArray());
        }
    }
}
