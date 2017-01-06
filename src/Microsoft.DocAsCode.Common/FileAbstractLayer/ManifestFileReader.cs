﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;

    public class ManifestFileReader : IFileReader
    {
        public Manifest Manifest { get; }

        public string ManifestFolder { get; }

        public ManifestFileReader(Manifest manifest, string manifestFolder)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            Manifest = manifest;
            ManifestFolder = manifestFolder;
        }

        #region IFileReader Members

        public PathMapping? FindFile(RelativePath file)
        {
            OutputFileInfo entry;
            lock (Manifest)
            {
                entry = FindEntryInManifest(file.RemoveWorkingFolder());
            }
            if (entry == null)
            {
                return null;
            }
            return new PathMapping(file, entry.LinkToPath ?? Path.Combine(ManifestFolder, entry.RelativePath));
        }

        public IEnumerable<RelativePath> EnumerateFiles()
        {
            lock (Manifest)
            {
                return (from f in Manifest.Files
                        from ofi in f.OutputFiles.Values
                        select ((RelativePath)ofi.RelativePath).GetPathFromWorkingFolder()).Distinct().ToList();
            }
        }

        #endregion

        private OutputFileInfo FindEntryInManifest(string file)
        {
            return (from f in Manifest.Files
                    from ofi in f.OutputFiles.Values
                    where ofi.RelativePath == file
                    select ofi).FirstOrDefault();
        }
    }
}
