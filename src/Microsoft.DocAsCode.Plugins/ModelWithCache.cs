// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Immutable;
    using System.Dynamic;
    using System.IO;
    using System.Runtime.Serialization;

    public sealed class ModelWithCache : IDisposable
    {
        private object _content;
        private FileStream _tempFile;
        private readonly WeakReference<object> _weakRef = new WeakReference<object>(null);
        private readonly IFormatter _serializer;
        public event EventHandler ContentAccessed;
        public string File { get; set; }
        public object Content
        {
            get
            {
                if (_content == null &&
                    _serializer != null &&
                    !_weakRef.TryGetTarget(out _content) &&
                    _tempFile != null)
                {
                    Deserialize();
                }
                OnContentAccessed();
                return _content;
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
            _serializer = serializer;
        }

        public bool Serialize()
        {
            if (_content == null || _serializer == null)
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
            _serializer.Serialize(_tempFile, _content);
            _weakRef.SetTarget(_content);
            _content = null;
            return true;
        }

        public bool Deserialize()
        {
            if (_tempFile == null || _serializer == null)
            {
                return false;
            }
            _tempFile.Seek(0, SeekOrigin.Begin);
            _content = _serializer.Deserialize(_tempFile);
            _weakRef.SetTarget(null);
            return true;
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
            var handler = ContentAccessed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private FileStream CreateTempFile()
        {
            return new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose | FileOptions.SequentialScan);
        }
    }
}
