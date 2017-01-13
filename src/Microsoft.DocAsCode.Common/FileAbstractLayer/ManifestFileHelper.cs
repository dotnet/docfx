// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;

    public static class ManifestFileHelper
    {
        public static bool AddFile(this Manifest manifest, string sourceFilePath, string extension, string targetRelativePath)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (sourceFilePath == null)
            {
                throw new ArgumentNullException(nameof(sourceFilePath));
            }
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }
            if (targetRelativePath == null)
            {
                throw new ArgumentNullException(nameof(targetRelativePath));
            }
            if (sourceFilePath.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty.", nameof(sourceFilePath));
            }
            if (targetRelativePath.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty.", nameof(extension));
            }
            if (!targetRelativePath.EndsWith(extension))
            {
                throw new ArgumentException("targetRelativePath has incorrect extension.", nameof(targetRelativePath));
            }

            lock (manifest)
            {
                foreach (var f in manifest.Files)
                {
                    if (f.SourceRelativePath == sourceFilePath)
                    {
                        f.OutputFiles[extension] = new OutputFileInfo
                        {
                            RelativePath = targetRelativePath,
                        };
                        return true;
                    }
                }
            }
            return false;
        }

        public static void RemoveLinks(this Manifest manifest, string manifestFolder)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (manifestFolder == null)
            {
                throw new ArgumentNullException(nameof(manifestFolder));
            }
            FileWriterBase.EnsureFolder(manifestFolder);
            lock (manifest)
            {
                var ofiList = (from f in manifest.Files
                               from ofi in f.OutputFiles.Values
                               where ofi.LinkToPath != null
                               select ofi).ToList();
                if (ofiList.Count == 0)
                {
                    return;
                }
                var fal = FileAbstractLayerBuilder.Default
                    .ReadFromManifest(manifest, manifestFolder)
                    .WriteToRealFileSystem(manifestFolder)
                    .Create();
                foreach (var rp in ofiList)
                {
                    fal.Copy(rp.RelativePath, rp.RelativePath);
                }
                foreach (var ofi in ofiList)
                {
                    ofi.LinkToPath = null;
                }
            }
        }
    }
}
