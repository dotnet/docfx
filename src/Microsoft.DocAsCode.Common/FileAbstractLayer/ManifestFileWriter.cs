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
        private readonly bool _noRandomFile;

        public Manifest Manifest { get; }

        public string ManifestFolder { get; }

        public ManifestFileWriter(Manifest manifest, string manifestFolder, string outputFolder)
            : base(outputFolder ?? manifestFolder)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (manifestFolder == null)
            {
                throw new ArgumentNullException(nameof(manifestFolder));
            }
            Manifest = manifest;
            ManifestFolder = manifestFolder;
            _noRandomFile = outputFolder == null;
        }

        #region Overrides

        public override void Copy(PathMapping sourceFileName, RelativePath destFileName)
        {
            lock (Manifest)
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
            lock (Manifest)
            {
                var entry = FindEntryInManifest(file.RemoveWorkingFolder());
                if (entry == null)
                {
                    throw new InvalidOperationException("File entry not found.");
                }
                if (_noRandomFile)
                {
                    Directory.CreateDirectory(
                        Path.Combine(ManifestFolder, file.RemoveWorkingFolder().GetDirectoryPath()));
                    var result = File.Create(Path.Combine(ManifestFolder, file.RemoveWorkingFolder()));
                    entry.LinkToPath = null;
                    return result;
                }
                else
                {
                    var pair = CreateRandomFileStream();
                    entry.LinkToPath = Path.Combine(OutputFolder, pair.Item1);
                    return pair.Item2;
                }
            }
        }

        public override IFileReader CreateReader()
        {
            return new ManifestFileReader(Manifest, ManifestFolder);
        }

        #endregion

        private OutputFileInfo FindEntryInManifest(string file)
        {
            return Manifest.FindOutputFileInfo(file);
        }
    }
}
