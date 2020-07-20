// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.LearnValidation
{
    public class LearnValidationConfig
    {
        public readonly string RepoUrl;
        public readonly string RepoBranch;
        public readonly string DocsetName;
        public readonly string DocsetPath;
        public readonly string PublishFilePath;
        public readonly string DependencyFilePath;
        public readonly string ManifestFilePath;
        public readonly string Environment;
        public readonly string FallbackDocsetPath;
        public readonly bool IsLocalizationBuild;

        public LearnValidationConfig(
            string repoUrl,
            string repoBranch,
            string docsetName,
            string docsetPath,
            string publishFilePath,
            string dependencyFilePath,
            string manifestFilePath,
            string environment,
            string fallbackDocsetPath,
            bool isLocalizationBuild)
        {
            RepoUrl = repoUrl;
            RepoBranch = repoBranch;
            DocsetName = docsetName;
            DocsetPath = docsetPath;
            PublishFilePath = publishFilePath;
            DependencyFilePath = dependencyFilePath;
            ManifestFilePath = manifestFilePath;
            Environment = environment;
            FallbackDocsetPath = fallbackDocsetPath;
            IsLocalizationBuild = isLocalizationBuild;
        }
    }
}
