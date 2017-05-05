// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Microsoft.DocAsCode.Common;

    public class FileCacheLite : IDisposable
    {
        public static readonly FileCacheLite Default = new FileCacheLite(new FilePathComparer());

        private readonly IDictionary<string, FileCacheModel> _cache;

        public FileCacheLite(IEqualityComparer<string> keyComparer)
        {
            _cache = new Dictionary<string, FileCacheModel>(keyComparer);
        }

        public void Add(string key, string value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(key);
            }
            var content = value ?? throw new ArgumentNullException(value);
            if (!_cache.TryGetValue(key, out FileCacheModel fm))
            {
                fm = new FileCacheModel();
                _cache[key] = fm;
            }

            fm.Content = content;
        }

        public string Get(string key)
        {
            TryGet(key, out string value);
            return value;
        }

        public bool Remove(string key)
        {
            if (_cache.TryGetValue(key, out FileCacheModel fm))
            {
                fm.Dispose();
                return _cache.Remove(key);
            }

            return false;
        }

        public bool TryGet(string key, out string value)
        {
            if (_cache.TryGetValue(key, out FileCacheModel fm))
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

        private sealed class FileCacheModel : IDisposable
        {
            private readonly WeakReference<string> _weakReference;
            private readonly FileStream _fs;

            public string Content
            {
                get
                {
                    if (!_weakReference.TryGetTarget(out string content))
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
                    _fs.SetLength(0);
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
                const int MaxRetry = 3;
                int retry = 0;
                while (true)
                {
                    try
                    {
                        var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                        return new FileStream(file, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose | FileOptions.SequentialScan);
                    }
                    catch (IOException)
                    {
                        if (retry++ < MaxRetry)
                        {
                            continue;
                        }
                        throw;
                    }
                }
            }

            public void Dispose()
            {
                _fs.Dispose();
            }
        }
    }
}
