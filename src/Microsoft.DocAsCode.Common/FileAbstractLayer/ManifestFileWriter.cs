// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;

    public class ManifestFileWriter : FileWriterBase
    {
        private readonly object _syncRoot;

        public Manifest Manifest { get; }

        public string ManifestFolder { get; }

        public ManifestFileWriter(Manifest manifest, string manifestFolder, string outputFolder)
            : this(manifest, manifestFolder, outputFolder, new object()) { }

        public ManifestFileWriter(Manifest manifest, string manifestFolder, string outputFolder, object syncRoot)
            : base(outputFolder)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (syncRoot == null)
            {
                throw new ArgumentNullException(nameof(syncRoot));
            }
            Manifest = manifest;
            ManifestFolder = manifestFolder;
            _syncRoot = syncRoot;
        }

        #region Overrides

        public override void Copy(PathMapping sourceFileName, RelativePath destFileName)
        {
            lock (_syncRoot)
            {
                var entry = FindEntryInManifest(destFileName.RemoveWorkingFolder());
                if (entry == null)
                {
                    throw new InvalidOperationException("File entry not found.");
                }
                var pair = CreateRandomFileStream();
                entry.LinkToPath = sourceFileName.PhysicalPath;
            }
        }

        public override Stream Create(RelativePath file)
        {
            lock (_syncRoot)
            {
                var entry = FindEntryInManifest(file.RemoveWorkingFolder());
                if (entry == null)
                {
                    throw new InvalidOperationException("File entry not found.");
                }
                var pair = CreateRandomFileStream();
                entry.LinkToPath = pair.Item1;
                return pair.Item2;
            }
        }

        public override IFileReader CreateReader()
        {
            return new ManifestFileReader(Manifest, ManifestFolder, _syncRoot);
        }

        #endregion

        private OutputFileInfo FindEntryInManifest(string file)
        {
            return (from f in Manifest.Files
                    from ofi in f.OutputFiles.Values
                    where  ofi.RelativePath == file
                    select ofi).FirstOrDefault();
        }
    }
}
