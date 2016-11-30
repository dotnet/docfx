// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class FileAbstractLayer : IFileAbstractLayer
    {
        #region Constructors

        public FileAbstractLayer(IFileReader reader, IFileWriter writer)
        {
            Reader = reader;
            Writer = writer;
        }

        #endregion

        #region Public Members

        public static FileAbstractLayer CreateLink(params PathMapping[] mappings)
        {
            return new FileAbstractLayer(new LinkFileReader(mappings), null);
        }

        public static FileAbstractLayer CreateLink(IEnumerable<PathMapping> mappings)
        {
            return new FileAbstractLayer(new LinkFileReader(mappings), null);
        }

        public static FileAbstractLayer CreateLink(IEnumerable<PathMapping> mappings, string outputFolder)
        {
            return new FileAbstractLayer(new LinkFileReader(mappings), new LinkFileWriter(outputFolder));
        }

        public IFileReader Reader { get; }

        public IFileWriter Writer { get; }

        #endregion

        #region IFileAbstractLayer Members

        public bool CanWrite => Writer != null;

        public IEnumerable<RelativePath> GetAllInputFiles() =>
            Reader.EnumerateFiles();

        public IEnumerable<RelativePath> GetAllOutputFiles()
        {
            if (!CanWrite)
            {
                throw new InvalidOperationException();
            }
            return Writer.CreateReader().EnumerateFiles();
        }

        public bool Exists(RelativePath file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            return Reader.FindFile(file) != null;
        }

        public FileStream OpenRead(RelativePath file)
        {
            var pp = FindPhysicalPath(file);
            return File.OpenRead(pp.PhysicalPath);
        }

        public FileStream Create(RelativePath file)
        {
            if (!CanWrite)
            {
                throw new InvalidOperationException();
            }
            return Writer.Create(file);
        }

        public void Copy(RelativePath sourceFileName, RelativePath destFileName)
        {
            if (!CanWrite)
            {
                throw new InvalidOperationException();
            }
            var mapping = FindPhysicalPath(sourceFileName);
            Writer.Copy(mapping, destFileName);
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

        #endregion
    }
}
