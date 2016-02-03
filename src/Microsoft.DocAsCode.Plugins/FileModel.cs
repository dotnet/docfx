// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Immutable;
    using System.Dynamic;
    using System.IO;
    using System.Runtime.Serialization;

    public sealed class FileModel : IDisposable
    {
        private readonly WeakReference<object> _weakRef = new WeakReference<object>(null);
        private readonly IFormatter _serializer;
        private ImmutableArray<string> _uids = ImmutableArray<string>.Empty;
        private object _content;
        private FileStream _tempFile;

        public FileModel(FileAndType ft, object content, FileAndType original = null, IFormatter serializer = null)
        {
            OriginalFileAndType = original ?? ft;

            if (OriginalFileAndType.File.StartsWith("~/"))
            {
                Key = OriginalFileAndType.File;
            }
            else
            {
                Key = "~/" + OriginalFileAndType.File;
            }

            FileAndType = ft;
            _content = content;
            _serializer = serializer;
        }

        public FileAndType FileAndType { get; private set; }

        public FileAndType OriginalFileAndType { get; private set; }

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

        public string BaseDir
        {
            get
            {
                return FileAndType.BaseDir;
            }
            set
            {
                if (value != BaseDir)
                {
                    FileAndType = new FileAndType(value, File, Type, PathRewriter);
                    OnFileOrBaseDirChanged();
                }
            }
        }

        public string File
        {
            get
            {
                return FileAndType.File;
            }
            set
            {
                if (value != File)
                {
                    FileAndType = new FileAndType(BaseDir, value, Type, PathRewriter);
                    OnFileOrBaseDirChanged();
                }
            }
        }

        public DocumentType Type => FileAndType.Type;

        public string Key { get; }

        public Func<string, string> PathRewriter => FileAndType.PathRewriter;

        public dynamic Properties { get; } = new ExpandoObject();

        public string LocalPathFromRepoRoot { get; set; }

        public string DocumentType { get; set; }

        public ImmutableArray<string> Uids
        {
            get { return _uids; }
            set
            {
                var original = _uids;
                _uids = value;
                OnUidsChanged(nameof(Uids), original, value);
            }
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

        public event EventHandler<PropertyChangedEventArgs<ImmutableArray<string>>> UidsChanged;

        public event EventHandler FileOrBaseDirChanged;

        public event EventHandler ContentAccessed;

        public void Dispose()
        {
            if (_tempFile != null)
            {
                _tempFile.Close();
                _tempFile = null;
            }
        }

        private FileStream CreateTempFile()
        {
            return new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose | FileOptions.SequentialScan);
        }

        private void OnUidsChanged(string propertyName, ImmutableArray<string> original, ImmutableArray<string> current)
        {
            var handler = UidsChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs<ImmutableArray<string>>(propertyName, original, current));
            }
        }

        private void OnFileOrBaseDirChanged()
        {
            var handler = FileOrBaseDirChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
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
    }
}
