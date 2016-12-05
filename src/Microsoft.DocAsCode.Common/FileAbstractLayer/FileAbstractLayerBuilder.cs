// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Immutable;

    public class FileAbstractLayerBuilder
    {
        private readonly IFileReader _reader;
        private readonly IFileWriter _writer;

        public FileAbstractLayerBuilder()
            : this(EmptyFileReader.Instance, null) { }

        private FileAbstractLayerBuilder(IFileReader reader, IFileWriter writer)
        {
            _reader = reader;
            _writer = writer;
        }

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

        public FileAbstractLayerBuilder WriteToRealFileSystem(string folder)
        {
            if (folder == null)
            {
                throw new ArgumentNullException(nameof(folder));
            }
            return new FileAbstractLayerBuilder(_reader, new RealFileWriter(folder));
        }

        public FileAbstractLayerBuilder ReadFromLink(params PathMapping[] mappings)
        {
            if (mappings == null)
            {
                throw new ArgumentNullException(nameof(mappings));
            }
            return new FileAbstractLayerBuilder(new LinkFileReader(mappings), _writer);
        }

        public FileAbstractLayerBuilder WriteToLink(string folder)
        {
            if (folder == null)
            {
                throw new ArgumentNullException(nameof(folder));
            }
            return new FileAbstractLayerBuilder(_reader, new LinkFileWriter(folder));
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

        public FileAbstractLayer Create()
        {
            return new FileAbstractLayer(_reader, _writer);
        }
    }
}
