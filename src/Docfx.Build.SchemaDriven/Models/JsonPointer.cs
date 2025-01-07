// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Exceptions;

namespace Docfx.Build.SchemaDriven;

/// <summary>
/// Json Pointer: https://tools.ietf.org/html/rfc6901
/// </summary>
public class JsonPointer
{
    private const string Splitter = "/";
    private readonly string[] _parts;
    private readonly bool _isRoot;
    private readonly string _raw;

    public JsonPointer(string raw)
    {
        raw ??= string.Empty;
        _isRoot = raw.Length == 0;
        if (!_isRoot && raw[0] != Splitter[0])
        {
            throw new InvalidJsonPointerException($"Invalid json pointer \"{raw}\"");
        }

        _parts = _isRoot ? [] : raw.Substring(1).Split(Splitter[0]);

        _raw = raw;
    }

    public JsonPointer(string[] parts)
    {
        _isRoot = parts == null || parts.Length == 0;
        _parts = parts ?? [];
        _raw = Splitter + string.Join(Splitter, parts);
    }

    public JsonPointer GetParentPointer()
    {
        if (_isRoot)
        {
            return null;
        }

        return new JsonPointer(_parts.Take(_parts.Length - 1).ToArray());
    }

    public static bool TryCreate(string raw, out JsonPointer pointer)
    {
        pointer = null;
        if (raw is { Length: > 0 } && raw[0] != '/')
        {
            return false;
        }
        pointer = new JsonPointer(raw);
        return true;
    }

    public BaseSchema FindSchema(DocumentSchema rootSchema)
    {
        if (_isRoot)
        {
            return rootSchema;
        }

        BaseSchema schema = rootSchema;
        foreach (var part in _parts)
        {
            schema = GetChildSchema(schema, part);
        }

        return schema;
    }

    public object GetValue(object root)
    {
        object val = root;
        foreach (var part in _parts)
        {
            val = GetChild(val, part);
            if (val == null)
            {
                return null;
            }
        }

        return val;
    }

    public void SetValue(ref object root, object value)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (_isRoot)
        {
            root = value;
            return;
        }

        object val = root;
        foreach (var part in _parts.Take(_parts.Length - 1))
        {
            val = GetChild(val, part);
            if (val == null)
            {
                throw new InvalidJsonPointerException("Unable to set value to null parent");
            }
        }

        SetChild(val, _parts[_parts.Length - 1], value);
    }

    public override string ToString()
    {
        return _raw ?? string.Empty;
    }

    public static object GetChild(object root, string part)
    {
        ArgumentNullException.ThrowIfNull(part);

        if (root == null)
        {
            return null;
        }

        var unescapedPart = UnescapeReference(part);
        if (int.TryParse(unescapedPart, out int index))
        {
            if (root is IList<object> list && list.Count > index)
            {
                return list[index];
            }
            else
            {
                return null;
            }
        }

        if (root is IDictionary<string, object> dict && dict.TryGetValue(unescapedPart, out object value))
        {
            return value;
        }

        if (root is IDictionary<object, object> objDict && objDict.TryGetValue(unescapedPart, out value))
        {
            return value;
        }

        return null;
    }

    public static void SetChild(object parent, string part, object value)
    {
        ArgumentNullException.ThrowIfNull(part);
        ArgumentNullException.ThrowIfNull(parent);

        var unescapedPart = UnescapeReference(part);
        if (int.TryParse(unescapedPart, out int index))
        {
            if (parent is IList<object> list)
            {
                if (list.Count < index)
                {
                    throw new InvalidJsonPointerException($"Unable to set value {index} beyond the index range of the array {list.Count}");
                }
                else if (list.Count == index)
                {
                    list.Add(value);
                }
                else
                {
                    list[index] = value;
                }
            }
        }
        else if (parent is IDictionary<string, object> dict)
        {
            dict[unescapedPart] = value;
        }
        else if (parent is IDictionary<object, object> objDict)
        {
            objDict[unescapedPart] = value;
        }
    }

    public static BaseSchema GetChildSchema(BaseSchema parent, string part)
    {
        ArgumentNullException.ThrowIfNull(part);

        if (parent == null)
        {
            return null;
        }

        if (part == null)
        {
            return null;
        }

        var unescapedPart = UnescapeReference(part);
        if (int.TryParse(unescapedPart, out int index))
        {
            return parent.Items;
        }

        return parent.Properties.GetValueOrDefault(part);
    }

    private static string UnescapeReference(string reference)
    {
        return Uri.UnescapeDataString(reference).Replace("~1", "/").Replace("~0", "~");
    }
}
