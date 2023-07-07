// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

/// <summary>
/// File writer.
/// </summary>
public interface IFileWriter
{
    /// <summary>
    /// Create a file with logical file path.
    /// </summary>
    /// <param name="file">logical file path</param>
    /// <returns>file stream</returns>
    Stream Create(RelativePath file);
    /// <summary>
    /// Copy a file to logical file path.
    /// </summary>
    /// <param name="sourceFileName">Source file.</param>
    /// <param name="destFileName">Dest file (logical file path).</param>
    void Copy(PathMapping sourceFileName, RelativePath destFileName);
    /// <summary>
    /// Create a reader to read files in output.
    /// </summary>
    /// <returns>A file reader.</returns>
    IFileReader CreateReader();
}
