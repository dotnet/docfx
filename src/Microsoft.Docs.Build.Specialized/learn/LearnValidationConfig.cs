// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.LearnValidation;

public class LearnValidationConfig
{
    public string RepoUrl { get; }

    public string RepoBranch { get; }

    public string DocsetName { get; }

    public string DocsetOutputPath { get; }

    public string PublishFilePath { get; }

    public string ManifestFilePath { get; }

    public bool IsLocalizationBuild { get; }

    public bool NoDrySync { get; }

    public LearnValidationConfig(
        string repoUrl,
        string repoBranch,
        string docsetName,
        string docsetOutputPath,
        string publishFilePath,
        string manifestFilePath,
        bool isLocalizationBuild,
        bool noDrySync)
    {
        RepoUrl = repoUrl;
        RepoBranch = repoBranch;
        DocsetName = docsetName;
        DocsetOutputPath = docsetOutputPath;
        PublishFilePath = publishFilePath;
        ManifestFilePath = manifestFilePath;
        IsLocalizationBuild = isLocalizationBuild;
        NoDrySync = noDrySync;
    }
}
