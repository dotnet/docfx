// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.LearnValidation
{
    public class LearnValidationConfig
    {
        public string RepoUrl { get; }

        public string RepoBranch { get; }

        public string DocsetName { get; }

        public string DocsetPath { get; }

        public string DocsetOutputPath { get; }

        public string PublishFilePath { get; }

        public string DependencyFilePath { get; }

        public string ManifestFilePath { get; }

        public string Environment { get; }

        public string FallbackDocsetPath { get; }

        public bool IsLocalizationBuild { get; }

        public LearnValidationConfig(
            string repoUrl,
            string repoBranch,
            string docsetName,
            string docsetPath,
            string docsetOutputPath,
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
            DocsetOutputPath = docsetOutputPath;
            PublishFilePath = publishFilePath;
            DependencyFilePath = dependencyFilePath;
            ManifestFilePath = manifestFilePath;
            Environment = environment;
            FallbackDocsetPath = fallbackDocsetPath;
            IsLocalizationBuild = isLocalizationBuild;
        }
    }
}
