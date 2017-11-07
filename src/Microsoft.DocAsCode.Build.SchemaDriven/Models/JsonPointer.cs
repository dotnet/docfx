// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Exceptions;

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
            raw = raw ?? string.Empty;
            _isRoot = raw.Length == 0;
            if (!_isRoot && raw[0] != Splitter[0])
            {
                throw new InvalidJsonPointerException($"Invalid json pointer \"{raw}\"");
            }

            _parts = _isRoot ? new string[0] : raw.Substring(1).Split(Splitter[0]);

            _raw = raw;
        }

        public JsonPointer(string[] parts)
        {
            _isRoot = parts == null || parts.Length == 0;
            _parts = parts == null ? new string[0] : parts;
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
            if (raw != null && raw.Length > 0 && raw[0] != '/')
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
            foreach(var part in _parts)
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
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

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
                    throw new InvalidJsonPointerException($"Unable to set value to null parent");
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
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

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
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

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
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

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

            if (parent.Properties.TryGetValue(part, out var bs))
            {
                return bs;
            }

            return null;
        }

        private static string UnescapeReference(string reference)
        {
            return Uri.UnescapeDataString(reference).Replace("~1", "/").Replace("~0", "~");
        }
    }
}
