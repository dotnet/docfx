// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Plugins;

    public class FileAbstractLayerBuilder
    {
        #region Fields
        public static readonly FileAbstractLayerBuilder Default = new FileAbstractLayerBuilder(EmptyFileReader.Instance, null);
        private readonly IFileReader _reader;
        private readonly IFileWriter _writer;
        #endregion

        #region Constructors

        private FileAbstractLayerBuilder(IFileReader reader, IFileWriter writer)
        {
            _reader = reader;
            _writer = writer;
        }

        #endregion

        #region Read

        public FileAbstractLayerBuilder ReadFromRealFileSystem(string folder) =>
            ReadFromRealFileSystem(folder, ImmutableDictionary<string, string>.Empty);

        public FileAbstractLayerBuilder ReadFromRealFileSystem(string folder, ImmutableDictionary<string, string> properties)
        {
            if (folder == null)
            {
                throw new ArgumentNullException(nameof(folder));
            }
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            return new FileAbstractLayerBuilder(new RealFileReader(folder, properties), _writer);
        }

        public FileAbstractLayerBuilder ReadFromLink(params PathMapping[] mappings)
        {
            if (mappings == null)
            {
                throw new ArgumentNullException(nameof(mappings));
            }
            return new FileAbstractLayerBuilder(new LinkFileReader(mappings), _writer);
        }

        public FileAbstractLayerBuilder ReadFromManifest(Manifest manifest, string manifestFolder)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (manifestFolder == null)
            {
                throw new ArgumentNullException(nameof(manifestFolder));
            }
            return new FileAbstractLayerBuilder(new ManifestFileReader(manifest, manifestFolder), _writer);
        }

        public FileAbstractLayerBuilder WriteToManifest(Manifest manifest, string manifestFolder, string outputFolder = null)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (manifestFolder == null)
            {
                throw new ArgumentNullException(nameof(manifestFolder));
            }
            return new FileAbstractLayerBuilder(_reader, new ManifestFileWriter(manifest, manifestFolder, outputFolder));
        }

        public FileAbstractLayerBuilder FallbackReadFromInput(FileAbstractLayer fal)
        {
            if (fal == null)
            {
                throw new ArgumentNullException(nameof(fal));
            }
            return new FileAbstractLayerBuilder(CreateFallback(_reader, fal.Reader), _writer);
        }

        public FileAbstractLayerBuilder FallbackReadFromOutput(FileAbstractLayer fal)
        {
            if (fal == null)
            {
                throw new ArgumentNullException(nameof(fal));
            }
            if (!fal.CanWrite)
            {
                throw new ArgumentException("FileAbstractLayer cannot write.", nameof(fal));
            }
            return new FileAbstractLayerBuilder(CreateFallback(_reader, fal.Writer.CreateReader()), _writer);
        }

        public FileAbstractLayerBuilder ReadFromOutput(FileAbstractLayer fal)
        {
            if (fal == null)
            {
                throw new ArgumentNullException(nameof(fal));
            }
            if (!fal.CanWrite)
            {
                throw new ArgumentException("FileAbstractLayer cannot write.", nameof(fal));
            }
            return new FileAbstractLayerBuilder(fal.Writer.CreateReader(), _writer);
        }

        public FileAbstractLayerBuilder ReadWithFolderRedirection(FolderRedirectionManager frm)
        {
            if (frm == null)
            {
                throw new ArgumentNullException(nameof(frm));
            }
            return new FileAbstractLayerBuilder(new FileReaderWithFolderRedirection(_reader, frm), _writer);
        }

        #endregion

        #region Write

        public FileAbstractLayerBuilder WriteToRealFileSystem(string folder)
        {
            if (folder == null)
            {
                throw new ArgumentNullException(nameof(folder));
            }
            return new FileAbstractLayerBuilder(_reader, new RealFileWriter(folder));
        }

        public FileAbstractLayerBuilder WriteToLink(string folder)
        {
            if (folder == null)
            {
                throw new ArgumentNullException(nameof(folder));
            }
            return new FileAbstractLayerBuilder(_reader, new LinkFileWriter(folder));
        }

        #endregion

        #region Create

        public FileAbstractLayer Create()
        {
            return new FileAbstractLayer(_reader, _writer);
        }

        public static FileAbstractLayerBuilder CreateBuilder(FileAbstractLayer fal)
        {
            if (fal == null)
            {
                throw new ArgumentNullException(nameof(fal));
            }
            return new FileAbstractLayerBuilder(fal.Reader, fal.Writer);
        }

        #endregion

        #region Private Methods

        private static IFileReader CreateFallback(IFileReader first, IFileReader second)
        {
            if (first == EmptyFileReader.Instance)
            {
                return second;
            }
            if (second == EmptyFileReader.Instance)
            {
                return first;
            }
            return CreateFallbackReader(first, second);
        }

        private static IFileReader CreateFallbackReader(IFileReader first, IFileReader second)
        {
            ImmutableArray<IFileReader> readers;
            if (first is FallbackFileReader fallbackReader)
            {
                readers = fallbackReader.Readers;
            }
            else
            {
                readers = ImmutableArray.Create(first);
            }
            fallbackReader = second as FallbackFileReader;
            if (fallbackReader != null)
            {
                readers = readers.AddRange(fallbackReader.Readers);
            }
            else
            {
                readers = readers.Add(second);
            }
            return new FallbackFileReader(readers);
        }

        #endregion
    }
}
