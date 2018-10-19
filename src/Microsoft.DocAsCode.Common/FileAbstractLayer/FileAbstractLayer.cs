// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;

    public class FileAbstractLayer : IFileAbstractLayer, IDisposable
    {
        #region Constructors

        public FileAbstractLayer(IFileReader reader, IFileWriter writer)
        {
            Reader = reader ?? throw new ArgumentNullException(nameof(reader));
            Writer = writer;
        }

        #endregion

        #region Public Members

        public IFileReader Reader { get; }

        public IFileWriter Writer { get; }

        public bool CanRead => !_disposed;

        public bool CanWrite => !_disposed && Writer != null;

        public IEnumerable<RelativePath> GetAllInputFiles()
        {
            EnsureNotDisposed();
            return Reader.EnumerateFiles();
        }

        public bool Exists(RelativePath file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            EnsureNotDisposed();
            return Reader.FindFile(file) != null;
        }

        public Stream OpenRead(RelativePath file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            EnsureNotDisposed();
            var pp = FindPhysicalPath(file);
            return File.OpenRead(Environment.ExpandEnvironmentVariables(pp.PhysicalPath));
        }

        public Stream Create(RelativePath file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            EnsureNotDisposed();
            if (!CanWrite)
            {
                throw new InvalidOperationException();
            }
            return Writer.Create(file);
        }

        public void Copy(RelativePath sourceFileName, RelativePath destFileName)
        {
            if (sourceFileName == null)
            {
                throw new ArgumentNullException(nameof(sourceFileName));
            }
            if (destFileName == null)
            {
                throw new ArgumentNullException(nameof(destFileName));
            }
            EnsureNotDisposed();
            if (!CanWrite)
            {
                throw new InvalidOperationException();
            }
            Writer.Copy(FindPhysicalPath(sourceFileName), destFileName);
        }

        public ImmutableDictionary<string, string> GetProperties(RelativePath file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            EnsureNotDisposed();
            return FindPhysicalPath(file).Properties;
        }

        public string GetPhysicalPath(RelativePath file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            EnsureNotDisposed();
            return FindPhysicalPath(file).PhysicalPath;
        }

        public IEnumerable<string> GetExpectedPhysicalPath(RelativePath file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            EnsureNotDisposed();
            return Reader.GetExpectedPhysicalPath(file);
        }

        public string CreateRandomFileName()
        {
            EnsureNotDisposed();
            if (!CanWrite)
            {
                throw new InvalidOperationException();
            }
            if (!(Writer is ISupportRandomFileWriter srfw))
            {
                throw new NotSupportedException();
            }
            return srfw.CreateRandomFileName();
        }

        public Tuple<string, Stream> CreateRandomFile()
        {
            EnsureNotDisposed();
            if (!CanWrite)
            {
                throw new InvalidOperationException();
            }
            var srfw = Writer as ISupportRandomFileWriter;
            if (srfw == null)
            {
                throw new NotSupportedException();
            }
            return srfw.CreateRandomFile();
        }

        #endregion

        #region IFileAbstractLayer Members

        IEnumerable<string> IFileAbstractLayer.GetAllInputFiles() =>
            from r in GetAllInputFiles()
            select (string)r.RemoveWorkingFolder();

        public bool Exists(string file) =>
            Exists((RelativePath)file);

        public Stream OpenRead(string file) =>
            OpenRead((RelativePath)file);

        public Stream Create(string file) =>
            Create((RelativePath)file);

        public void Copy(string sourceFileName, string destFileName) =>
            Copy((RelativePath)sourceFileName, (RelativePath)destFileName);

        public ImmutableDictionary<string, string> GetProperties(string file) =>
            GetProperties((RelativePath)file);

        public string GetPhysicalPath(string file) =>
            GetPhysicalPath((RelativePath)file);

        public IEnumerable<string> GetExpectedPhysicalPath(string file) =>
            GetExpectedPhysicalPath((RelativePath)file);

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #region Private Methods

        private PathMapping FindPhysicalPath(RelativePath file)
        {
            var mapping = Reader.FindFile(file);
            if (mapping == null)
            {
                string fn = file;
                throw new FileNotFoundException($"File ({fn}) not found.", fn);
            }
            return mapping.Value;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("FileAbstractLayer");
            }
        }

        #endregion
    }
}
