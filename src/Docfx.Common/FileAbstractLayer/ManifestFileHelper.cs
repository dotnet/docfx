// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Common;

public static class ManifestFileHelper
{
    public static void Dereference(this Manifest manifest, string manifestFolder, int parallelism)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(manifestFolder);

        FileWriterBase.EnsureFolder(manifestFolder);
        var fal = FileAbstractLayerBuilder.Default
            .ReadFromManifest(manifest, manifestFolder)
            .WriteToRealFileSystem(manifestFolder)
            .Create();
        Parallel.ForEach(
            from f in manifest.Files
            from ofi in f.Output.Values
            where ofi.LinkToPath != null
            select ofi,
            new ParallelOptions { MaxDegreeOfParallelism = parallelism },
            ofi =>
            {
                fal.Copy(ofi.RelativePath, ofi.RelativePath);
                ofi.LinkToPath = null;
            });
    }
}
