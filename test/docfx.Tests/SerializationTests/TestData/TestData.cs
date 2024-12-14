// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.ApiPage;
using Docfx.Common;
using Docfx.Tests;

namespace docfx.Tests;

public static class TestData
{
    /// <summary>
    /// Load test data from specified path.
    /// </summary>
    public static T Load<T>(string path)
    {
        if (typeof(T) == typeof(ApiPage))
            throw new NotSupportedException(); // It should be handled separately.

        var testDataPath = PathHelper.ResolveTestDataPath(path);

        switch (Path.GetExtension(path))
        {
            case ".yml":
                return YamlUtility.Deserialize<T>(testDataPath);
            case ".json":
                return JsonUtility.Deserialize<T>(testDataPath);
            default:
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Gets test data relative file paths.
    /// </summary>
    public static string[] GetTestDataFilePaths(string key)
    {
        var testDataRootDir = PathHelper.GetTestDataDirectory();
        var basePathLength = testDataRootDir.Length + 1;

        var testDataDir = Path.Combine(testDataRootDir, key);
        var directoryInfo = new DirectoryInfo(testDataDir);

        // Gets TestData directory relative paths
        var relativePaths = directoryInfo.EnumerateFiles("*", new EnumerationOptions { RecurseSubdirectories = true })
                                         .Select(x => x.FullName)
                                         .Select(x => x.Substring(basePathLength));

        return relativePaths.ToArray();
    }
}
