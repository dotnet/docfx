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
        private ImmutableArray<string> _uids = ImmutableArray<string>.Empty;
        
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
            ModelWithCache = new ModelWithCache(content, serializer);
        }

        public FileAndType FileAndType { get; private set; }

        public FileAndType OriginalFileAndType { get; private set; }
        public ModelWithCache ModelWithCache { get; }
        public object Content
        {
            get
            {
                return ModelWithCache.Content;
            }
            set
            {
                ModelWithCache.Content = value;
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

        public string LocalPathFromRepoRoot
        {
            get
            {
                return ModelWithCache.File;
            }
            set
            {
                ModelWithCache.File = value;
            }
        }

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
            return ModelWithCache.Serialize();
        }

        public bool Deserialize()
        {
            return ModelWithCache.Deserialize();
        }

        public event EventHandler<PropertyChangedEventArgs<ImmutableArray<string>>> UidsChanged;

        public event EventHandler FileOrBaseDirChanged;

        public event EventHandler ContentAccessed
        {
            add
            {
                ModelWithCache.ContentAccessed += value;
            }
            remove
            {
                ModelWithCache.ContentAccessed -= value;
            }
        }

        public void Dispose()
        {
            ModelWithCache.Dispose();
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
    }
}
