// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;

    public sealed class ModelWithCache : IDisposable
    {
        private object _content;
        private FileStream _tempFile;
        private readonly WeakReference<object> _weakRef = new WeakReference<object>(null);
        private readonly object _locker = new object();

        public event EventHandler ContentAccessed;

        public IFormatter Serializer { get; set; }

        public string File { get; set; }

        public object Content
        {
            get
            {
                if (_content == null &&
                    Serializer != null &&
                    !_weakRef.TryGetTarget(out _content) &&
                    _tempFile != null)
                {
                    Deserialize();
                }
                var content = _content;
                OnContentAccessed();
                return content;
            }
            set
            {
                if (_content == value)
                {
                    return;
                }
                _content = value;
                if (_tempFile != null)
                {
                    _tempFile.Close();
                    _tempFile = null;
                }
                OnContentAccessed();
            }
        }

        public ModelWithCache(object content, IFormatter serializer = null)
        {
            _content = content;
            Serializer = serializer;
        }

        public bool Serialize()
        {
            lock (_locker)
            {
                if (_content == null || Serializer == null)
                {
                    return false;
                }
                if (_tempFile == null)
                {
                    _tempFile = CreateTempFile();
                }
                else
                {
                    _tempFile.Seek(0, SeekOrigin.Begin);
                    _tempFile.SetLength(0);
                }
                Serializer.Serialize(_tempFile, _content);
                _weakRef.SetTarget(_content);
                _content = null;
                return true;
            }
        }

        public bool Deserialize()
        {
            lock (_locker)
            {
                if (_tempFile == null || Serializer == null)
                {
                    return false;
                }
                _tempFile.Seek(0, SeekOrigin.Begin);
                _content = Serializer.Deserialize(_tempFile);
                _weakRef.SetTarget(null);
                return true;
            }
        }

        public void Dispose()
        {
            if (_tempFile != null)
            {
                _tempFile.Close();
                _tempFile = null;
            }
        }

        private void OnContentAccessed()
        {
            ContentAccessed?.Invoke(this, EventArgs.Empty);
        }

        private FileStream CreateTempFile()
        {
            return new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose | FileOptions.SequentialScan);
        }
    }
}
