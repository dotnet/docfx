// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    public class IgnoreStrongTypeObjectJsonReader : JsonReader
    {
        private object _current => _currentNode?.Value;
        private Node _currentNode;
        private Node _parentNode;

        public IgnoreStrongTypeObjectJsonReader(object obj)
        {
            _currentNode = new Node(obj, null);
            _parentNode = null;
        }

        public override bool Read()
        {
            if (_currentNode == null)
            {
                return false;
            }
            else if (_current is IDictionary<string, object> sdict)
            {
                SetToken(JsonToken.StartObject);
                _currentNode = new DictNode<string, object>(sdict, _parentNode);
                return MoveToNext();
            }
            else if (_current is IDictionary<object, object> odict)
            {
                SetToken(JsonToken.StartObject);
                _currentNode = new DictNode<object, object>(odict, _parentNode);
                return MoveToNext();
            }
            else if (_current is IList<object> list)
            {
                SetToken(JsonToken.StartArray);
                _currentNode = new ArrayNode<object>(list, _parentNode);
                return MoveToNext();
            }
            else
            {
                var token = _currentNode.Token ?? GetJsonToken(_current);
                if (token == JsonToken.StartObject)
                {
                    _currentNode = new Node(_current, _parentNode);
                    // ignore strong type object in case plugins create some
                    SetToken(JsonToken.Null, null);
                }
                else
                {
                    SetToken(token.Value, _current);
                }

                return MoveToNext();
            }
        }

        private bool MoveToNext()
        {
            if (_currentNode == null)
            {
                return false;
            }

            // Try child first
            var child = _currentNode.NextChild();
            if (child != null)
            {
                _parentNode = _currentNode;
                _currentNode = child;
                return true;
            }

            if (_currentNode.Parent == null)
            {
                _currentNode = null;
                return true;
            }

            var next = _currentNode.Parent.NextChild();
            if (next == null)
            {
                _currentNode = _currentNode.Parent;
                _parentNode = _currentNode.Parent;
            }
            else
            {
                _currentNode = next;
            }
            return true;
        }

        private JsonToken? GetJsonToken(object obj)
        {
            if (obj is null)
            {
                return JsonToken.Null;
            }

            if (obj is IConvertible ct)
            {
                return GetJsonToken(ct.GetTypeCode());
            }

            return GetJsonToken(Type.GetTypeCode(obj.GetType()));
        }

        private JsonToken? GetJsonToken(TypeCode code)
        {
            switch (code)
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                    return JsonToken.Null;
                case TypeCode.Boolean:
                    return JsonToken.Boolean;
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return JsonToken.Bytes;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return JsonToken.Integer;
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return JsonToken.Float;
                case TypeCode.DateTime:
                    return JsonToken.Date;
                case TypeCode.Char:
                case TypeCode.String:
                    return JsonToken.String;
                case TypeCode.Object:
                    return JsonToken.StartObject;
                default:
                    throw new NotSupportedException($"{code} is not supported in {nameof(IgnoreStrongTypeObjectJsonReader)}");
            }
        }

        private class Node
        {
            public object Value;
            public Node Parent;
            public JsonToken? Token = null;

            public Node(object val, Node parent)
            {
                Value = val;
                Parent = parent;
            }

            public virtual Node NextChild()
            {
                return null;
            }
        }

        private class EmptyNode : Node
        {
            public EmptyNode(Node parent, JsonToken token) : base(null, parent)
            {
                Token = token;
            }

            public override Node NextChild()
            {
                return Parent?.NextChild();
            }
        }

        private class DictNode<TKey, TValue> : Node
        {
            public new readonly IList<KeyValuePair<TKey, TValue>> Value;

            private int index = 0;
            private bool firstRound = true;
            public DictNode(IDictionary<TKey, TValue> val, Node parent) : base(val, parent)
            {
                Value = val.ToList();
            }

            public override Node NextChild()
            {
                if (Value.Count == 0 || index > Value.Count - 1)
                {
                    return new EmptyNode(Parent, JsonToken.EndObject);
                }

                var child = Value[index];
                if (firstRound)
                {
                    firstRound = false;
                    return new Node(child.Key, this)
                    {
                        Token = JsonToken.PropertyName,
                    };
                }
                else
                {
                    firstRound = true;
                    index++;
                    return new Node(child.Value, this);
                }
            }
        }

        private class ArrayNode<T> : Node
        {
            public new readonly IList<T> Value;

            private int index = 0;
            public ArrayNode(IList<T> val, Node parent) : base(val, parent)
            {
                Value = val;
            }

            public override Node NextChild()
            {
                if (Value.Count == 0 || index > Value.Count - 1)
                {
                    return new EmptyNode(Parent, JsonToken.EndArray);
                }

                var child = Value[index];
                index++;
                return new Node(child, this);
            }
        }
    }
}
