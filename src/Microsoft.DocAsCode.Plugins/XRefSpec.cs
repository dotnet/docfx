namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    [Serializable]
    public sealed class XRefSpec : IDictionary<string, object>
    {
        public const string UidKey = "uid";
        public const string NameKey = "name";
        public const string HrefKey = "href";
        public const string CommentIdKey = "commentId";
        public const string IsSpecKey = "isSpec";

        private Dictionary<string, object> _dict;

        public XRefSpec()
        {
            _dict = new Dictionary<string, object>();
        }

        public XRefSpec(IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }
            _dict = new Dictionary<string, object>(dictionary);
        }

        public XRefSpec(XRefSpec spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }
            _dict = new Dictionary<string, object>(spec._dict);
        }

        public string Uid
        {
            get
            {
                _dict.TryGetValue(UidKey, out var value);
                return (string)value;
            }
            set { _dict[UidKey] = value; }
        }

        public string Name
        {
            get
            {
                _dict.TryGetValue(NameKey, out var value);
                return (string)value;
            }
            set { _dict[NameKey] = value; }
        }

        public string Href
        {
            get
            {
                _dict.TryGetValue(HrefKey, out var value);
                return (string)value;
            }
            set { _dict[HrefKey] = value; }
        }

        public string CommentId
        {
            get
            {
                _dict.TryGetValue(CommentIdKey, out var value);
                return (string)value;
            }
            set
            {
                if (value != null)
                {
                    _dict[CommentIdKey] = value;
                }
                else
                {
                    _dict.Remove(CommentIdKey);
                }
            }
        }

        public bool IsSpec
        {
            get
            {
                return _dict.ContainsKey(IsSpecKey);
            }
            set
            {
                if (value)
                {
                    _dict[IsSpecKey] = bool.TrueString;
                }
                else
                {
                    _dict.Remove(IsSpecKey);
                }
            }
        }

        private void ThrowIfReadOnly()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Read only.");
            }
        }

        public XRefSpec ToReadOnly()
        {
            if (IsReadOnly)
            {
                return this;
            }
            else
            {
                return new XRefSpec
                {
                    _dict = new Dictionary<string, object>(_dict),
                    IsReadOnly = true,
                };
            }
        }

        /// <summary>
        /// Merge two xref spec (right overwrite left).
        /// </summary>
        public static XRefSpec Merge(XRefSpec left, XRefSpec right)
        {
            if (left == null)
            {
                return right;
            }
            if (right == null)
            {
                return left;
            }
            var result = new XRefSpec(left);
            foreach (var pair in right._dict)
            {
                result._dict[pair.Key] = pair.Value;
            }
            return result;
        }

        public static XRefSpec operator +(XRefSpec left, XRefSpec right) => Merge(left, right);

        #region IDictionary<string, string> Members

        public object this[string key]
        {
            get { return _dict[key]; }
            set
            {
                ThrowIfReadOnly();
                _dict[key] = value;
            }
        }

        public int Count => _dict.Count;

        public bool IsReadOnly { get; private set; }

        public ICollection<string> Keys => _dict.Keys;

        public ICollection<object> Values => _dict.Values;

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            ThrowIfReadOnly();
            _dict.Add(item.Key, item.Value);
        }

        public void Add(string key, object value)
        {
            ThrowIfReadOnly();
            _dict.Add(key, value);
        }

        public void Clear()
        {
            ThrowIfReadOnly();
            _dict.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) => ((IDictionary<string, object>)_dict).Contains(item);

        public bool ContainsKey(string key) => _dict.ContainsKey(key);

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => ((IDictionary<string, object>)_dict).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _dict.GetEnumerator();

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            ThrowIfReadOnly();
            return ((IDictionary<string, object>)_dict).Remove(item);
        }

        public bool Remove(string key)
        {
            ThrowIfReadOnly();
            return _dict.Remove(key);
        }

        public bool TryGetValue(string key, out object value) => _dict.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();

        #endregion
    }
}
