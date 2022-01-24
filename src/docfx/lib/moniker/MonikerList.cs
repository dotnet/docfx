// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

[JsonConverter(typeof(MonikerListJsonConverter))]
internal readonly struct MonikerList : IEquatable<MonikerList>, IReadOnlyCollection<string>, IComparable<MonikerList>
{
    private static readonly ConcurrentDictionary<MonikerList, string> s_monikerGroupCache = new();

    private readonly string[]? _monikers;

    public int Count => _monikers is null ? 0 : _monikers.Length;

    public bool HasMonikers => _monikers != null && _monikers.Length > 0;

    public string? MonikerGroup => _monikers is null || _monikers.Length == 0
        ? null
        : s_monikerGroupCache.GetOrAdd(this, HashUtility.GetSha256HashShort(string.Join(',', _monikers)));

    public override string ToString() => _monikers is null ? "" : string.Join(", ", _monikers);

    public MonikerList(IEnumerable<string> monikers)
    {
        _monikers = monikers.Select(m => m.ToLowerInvariant()).Distinct().OrderBy(m => m, StringComparer.Ordinal).ToArray();
    }

    public bool IsCanonicalVersion(string? canonicalVersion)
    {
        if (_monikers is null || string.IsNullOrEmpty(canonicalVersion))
        {
            return true;
        }

        return _monikers.Contains(canonicalVersion);
    }

    public bool Intersects(MonikerList other)
    {
        if (_monikers is null || _monikers.Length == 0 || other._monikers is null || other._monikers.Length == 0)
        {
            return true;
        }

        return _monikers.Intersect(other._monikers).Any();
    }

    public MonikerList Intersect(MonikerList other)
    {
        if (_monikers is null || _monikers.Length == 0 || other._monikers is null || other._monikers.Length == 0)
        {
            return default;
        }

        return new(_monikers.Intersect(other._monikers));
    }

    public MonikerList Except(MonikerList other)
    {
        if (_monikers is null || _monikers.Length == 0 || other._monikers is null || other._monikers.Length == 0)
        {
            return this;
        }

        return new(_monikers.Except(other._monikers));
    }

    public static MonikerList Union(IEnumerable<MonikerList> monikerLists)
    {
        var monikers = new HashSet<string>();

        foreach (var monikerList in monikerLists)
        {
            if (monikerList._monikers is null || monikerList._monikers.Length == 0)
            {
                return default;
            }

            monikers.AddRange(monikerList._monikers);
        }

        return new(monikers);
    }

    public override int GetHashCode()
    {
        if (_monikers is null || _monikers.Length == 0)
        {
            return 0;
        }

        var hashCode = default(HashCode);
        hashCode.Add(_monikers.Length);

        for (var i = 0; i < _monikers.Length; i++)
        {
            hashCode.Add(_monikers[i].GetHashCode());
        }

        return hashCode.ToHashCode();
    }

    public bool Equals(MonikerList other)
    {
        if (_monikers is null || _monikers.Length == 0)
        {
            return other._monikers is null || other._monikers.Length == 0;
        }

        if (other._monikers is null || other._monikers.Length != _monikers.Length)
        {
            return false;
        }

        for (var i = 0; i < _monikers.Length; i++)
        {
            if (_monikers[i] != other._monikers[i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is MonikerList list && Equals(list);
    }

    public IEnumerator<string> GetEnumerator()
    {
        return (_monikers ?? Enumerable.Empty<string>()).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (_monikers ?? Array.Empty<string>()).GetEnumerator();
    }

    public int CompareTo(MonikerList other)
    {
        if (!HasMonikers && other.HasMonikers)
        {
            return 1;
        }
        else if (HasMonikers && !other.HasMonikers)
        {
            return -1;
        }

        return PathUtility.PathComparer.Compare(MonikerGroup, other.MonikerGroup);
    }

    public static bool operator ==(MonikerList left, MonikerList right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MonikerList left, MonikerList right)
    {
        return !(left == right);
    }

    private class MonikerListJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(MonikerList);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = serializer.Deserialize<string[]>(reader);
            return value is null ? default : new MonikerList(value);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is MonikerList monikerList)
            {
                writer.WriteStartArray();
                if (monikerList._monikers != null)
                {
                    foreach (var moniker in monikerList._monikers)
                    {
                        writer.WriteValue(moniker);
                    }
                }
                writer.WriteEndArray();
            }
        }
    }
}
