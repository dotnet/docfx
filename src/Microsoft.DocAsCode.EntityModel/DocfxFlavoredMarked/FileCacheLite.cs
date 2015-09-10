// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Utility;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class FileCacheLite : IDisposable
    {
        private IDictionary<string, FileCacheModel> _cache;

        public static FileCacheLite Default = new FileCacheLite(new FilePathComparer());

        public FileCacheLite(IEqualityComparer<string> keyComparer)
        {
            _cache = new Dictionary<string, FileCacheModel>(keyComparer);
        }

        public void Add(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(key);
            if (value == null) throw new ArgumentNullException(value);
            FileCacheModel fm;
            if (!_cache.TryGetValue(key, out fm))
            {
                fm = new FileCacheModel();
                _cache[key] = fm;
            }

            fm.Content = value;
        }

        public string Get(string key)
        {
            string value;
            if (TryGet(key, out value))
            {
                return value;
            }

            return null;
        }

        public bool Remove(string key)
        {
            FileCacheModel fm;
            if ( _cache.TryGetValue(key, out fm))
            {
                fm.Dispose();
                return _cache.Remove(key);
            }

            return false;
        }

        public bool TryGet(string key, out string value)
        {
            FileCacheModel fm;
            if (_cache.TryGetValue(key, out fm))
            {
                value = fm.Content;
                return true;
            }

            value = null;
            return false;
        }

        public void Dispose()
        {
            foreach (var fm in _cache)
            {
                fm.Value.Dispose();
            }
        }
    }

    internal sealed class FileCacheModel : IDisposable
    {
        private readonly WeakReference<string> _weakReference;
        private FileStream _fs;

        public string Content
        {
            get
            {
                string content;
                if (!_weakReference.TryGetTarget(out content))
                {
                    _fs.Seek(0, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(_fs, Encoding.UTF8, true, 4096, true))
                    {
                        string result = reader.ReadToEnd();
                        _weakReference.SetTarget(result);
                        return result;
                    }
                }

                return content;
            }
            set
            {
                _weakReference.SetTarget(value);
                using (StreamWriter writer = new StreamWriter(_fs, Encoding.UTF8, 4096, true))
                    writer.Write(value);
            }
        }

        public FileCacheModel()
        {
            _weakReference = new WeakReference<string>(null);
            _fs = CreateTempFile();
        }

        private FileStream CreateTempFile()
        {
            return new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose | FileOptions.SequentialScan);
        }

        public void Dispose()
        {
            _fs.Dispose();
        }
    }
}
