// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Common;

public class ManifestFileWriter : FileWriterBase
{
    private readonly bool _noRandomFile;
    private readonly Manifest _manifest;
    private readonly string _manifestFolder;
    private readonly Dictionary<string, OutputFileInfo> _files;

    public ManifestFileWriter(Manifest manifest, string manifestFolder, string outputFolder)
        : base(outputFolder ?? manifestFolder)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(manifestFolder);

        _manifest = manifest;
        _manifestFolder = manifestFolder;
        _noRandomFile = outputFolder == null;
        _files = ManifestFileReader.ToLookup(manifest);
    }

    public override void Copy(PathMapping sourceFileName, RelativePath destFileName)
    {
        lock (_manifest)
        {
            _files[destFileName.RemoveWorkingFolder()].LinkToPath = sourceFileName.PhysicalPath;
        }
    }

    public override Stream Create(RelativePath file)
    {
        lock (_manifest)
        {
            var entry = _files[file.RemoveWorkingFolder()];
            if (entry == null)
            {
                throw new InvalidOperationException("File entry not found.");
            }

            string path = _noRandomFile
                            ? Path.Combine(_manifestFolder, file.RemoveWorkingFolder())
                            : Path.Combine(OutputFolder, file.RemoveWorkingFolder());
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            for (int retryCount = 0; retryCount <= 3; retryCount++)
            {
                try
                {
                    var fileStream = File.Create(path);
                    entry.LinkToPath = null;
                    return fileStream;
                }
                catch (IOException e) when (e.HResult == -2147024864)
                {
                    // ERROR_SHARING_VIOLATION
                    if (retryCount >= 3)
                        throw;

                    // retry immediately on first exception
                    var sleepDelay = 500 * retryCount;
                    Logger.LogVerbose($"{e.Message}{Environment.NewLine}Retrying after {sleepDelay}ms...");
                    Thread.Sleep(sleepDelay);
                }
            }

            throw new InvalidOperationException($"Failed to create file \"{path}\".");
        }
    }

    public override IFileReader CreateReader()
    {
        return new ManifestFileReader(_manifest, _manifestFolder);
    }
}
