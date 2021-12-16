// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation;

public static class DependencyMapHelper
{
    public static List<DependencyItem> LoadDependentFileInfo(string dependencyMapFile)
    {
        var dependencyItems = new List<DependencyItem>();
        using (var stream = File.OpenRead(dependencyMapFile))
        using (var reader = new StreamReader(stream))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var dependentInfo = !string.IsNullOrEmpty(line) ? JsonConvert.DeserializeObject<DependencyItem>(line) : null;

                if (dependentInfo != null && !string.IsNullOrEmpty(dependentInfo.FromFilePath) && !string.IsNullOrEmpty(dependentInfo.ToFilePath))
                {
                    var normalizedToFilePath = dependentInfo.ToFilePath.Replace('/', '\\');
                    var normalizedFromFilePath = dependentInfo.FromFilePath.Replace('/', '\\');

                    dependencyItems.Add(
                        new DependencyItem
                        {
                            FromFilePath = normalizedFromFilePath,
                            ToFilePath = normalizedToFilePath,
                            DependencyType = dependentInfo.DependencyType,
                            Version = dependentInfo.Version,
                        });
                }
            }
        }

        return dependencyItems;
    }
}
