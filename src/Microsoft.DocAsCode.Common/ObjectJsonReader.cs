// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    public class ObjectJsonReader : JsonReader
    {
        private object _current => _currentNode?.Value;
        private Node _currentNode;
        private Node _parentNode;

        public ObjectJsonReader(object obj)
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
                SetToken(token.Value, _current);
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

            if (obj is string)
            {
                return JsonToken.String;
            }

            if (obj is bool)
            {
                return JsonToken.Boolean;
            }

            if (obj is int || obj is long)
            {
                return JsonToken.Integer;
            }

            if (obj is double || obj is float)
            {
                return JsonToken.Float;
            }

            if (obj is DateTime)
            {
                return JsonToken.Date;
            }

            throw new NotSupportedException($"{obj.GetType()} is not supported in {nameof(ObjectJsonReader)}");
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
                Parent = parent;
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
