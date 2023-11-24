// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

/// <summary>
/// File reader.
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Convert a logical file path to a physical file path
    /// </summary>
    /// <param name="file">Logical file path.</param>
    /// <returns>A path mapping.</returns>
    PathMapping? FindFile(RelativePath file);
    /// <summary>
    /// Get all files in this reader.
    /// </summary>
    /// <returns>A set of logical file path (from working folder).</returns>
    IEnumerable<RelativePath> EnumerateFiles();
    /// <summary>
    /// Get expected physical paths.
    /// </summary>
    /// <param name="file">Logical file path.</param>
    /// <returns>Expected physical paths.</returns>
    string GetExpectedPhysicalPath(RelativePath file);
}
