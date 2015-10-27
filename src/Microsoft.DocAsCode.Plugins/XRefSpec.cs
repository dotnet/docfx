namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public sealed class XRefSpec : IDictionary<string, string>
    {
        public const string UidKey = "uid";
        public const string NameKey = "name";

        private Dictionary<string, string> _dict = new Dictionary<string, string>();
        private bool _isReadOnly;

        public string Uid
        {
            get
            {
                string value;
                _dict.TryGetValue(UidKey, out value);
                return value;
            }
            set { _dict[UidKey] = value; }
        }

        public string Name
        {
            get
            {
                string value;
                _dict.TryGetValue(NameKey, out value);
                return value;
            }
            set { _dict[NameKey] = value; }
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
