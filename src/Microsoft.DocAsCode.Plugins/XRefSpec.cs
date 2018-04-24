namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    [Serializable]
    public sealed class XRefSpec : IDictionary<string, string>
    {
        public const string UidKey = "uid";
        public const string NameKey = "name";
        public const string HrefKey = "href";
        public const string CommentIdKey = "commentId";
        public const string IsSpecKey = "isSpec";

        private Dictionary<string, string> _dict;
        private bool _isReadOnly;

        public XRefSpec()
        {
            _dict = new Dictionary<string, string>();
        }

        public XRefSpec(IDictionary<string, string> dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }
            _dict = new Dictionary<string, string>(dictionary);
        }

        public XRefSpec(XRefSpec spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }
            _dict = new Dictionary<string, string>(spec._dict);
        }

        public string Uid
        {
            get
            {
                _dict.TryGetValue(UidKey, out string value);
                return value;
            }
            set { _dict[UidKey] = value; }
        }

        public string Name
        {
            get
            {
                _dict.TryGetValue(NameKey, out string value);
                return value;
            }
            set { _dict[NameKey] = value; }
        }

        public string Href
        {
            get
            {
                _dict.TryGetValue(HrefKey, out string value);
                return value;
            }
            set { _dict[HrefKey] = value; }
        }

        public string CommentId
        {
            get
            {
                _dict.TryGetValue(CommentIdKey, out string value);
                return value;
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
            if (_isReadOnly)
            {
                throw new InvalidOperationException("Read only.");
            }
        }

        public XRefSpec ToReadOnly()
        {
            if (_isReadOnly)
            {
                return this;
            }
            else
            {
                return new XRefSpec
                {
                    _dict = new Dictionary<string, string>(_dict),
                    _isReadOnly = true,
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

        public string this[string key]
        {
            get { return _dict[key]; }
            set
            {
                ThrowIfReadOnly();
                _dict[key] = value;
            }
        }

        public int Count => _dict.Count;

        public bool IsReadOnly => _isReadOnly;

        public ICollection<string> Keys => _dict.Keys;

        public ICollection<string> Values => _dict.Values;

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
        {
            ThrowIfReadOnly();
            _dict.Add(item.Key, item.Value);
        }

        public void Add(string key, string value)
        {
            ThrowIfReadOnly();
            _dict.Add(key, value);
        }

        public void Clear()
        {
            ThrowIfReadOnly();
            _dict.Clear();
        }

        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item) => ((IDictionary<string, string>)_dict).Contains(item);

        public bool ContainsKey(string key) => _dict.ContainsKey(key);

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((IDictionary<string, string>)_dict).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _dict.GetEnumerator();

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            ThrowIfReadOnly();
            return ((IDictionary<string, string>)_dict).Remove(item);
        }

        public bool Remove(string key)
        {
            ThrowIfReadOnly();
            return _dict.Remove(key);
        }

        public bool TryGetValue(string key, out string value) => _dict.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();

        #endregion
    }
}
