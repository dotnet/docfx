// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.LearnValidation
{
    public class CommandLineOptions
    {
        public string LogFilePath = "log.json";
        public string OriginalManifestPath = null;
        public string RepoRootPath = null;
        public string XRefEndpoint = null;
        public string XRefTags = null;
        public string Locale = null;
        public string Branch = null;
        public string TripleCrownEndpoint = null;
        public string DependencyFilePath = null;
        public string DocsetFolder = null;
        public string DocsetName = null;
        public string DrySyncEndpoint = null;
        public string FallbackFolders = null;
        public string RepoUrl = null;
        public string SkipPublishFilePath = null;
        public bool IsServerBuild = false;
        public bool ContinueWithError = false;

        public bool Parse(string[] args)
        {
            if (string.IsNullOrEmpty(DependencyFilePath) || !File.Exists(DependencyFilePath))
            {
                Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_DependencyFile_NotExist);
                return false;
            }
            if (string.IsNullOrEmpty(DocsetFolder))
            {
                Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_DocsetFolder_IsNull);
                return false;
            }
            if (string.Compare(Locale, "en-us", true) != 0 && string.IsNullOrEmpty(RepoRootPath))
            {
                Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_RepoRootPath_IsNull);
                return false;
            }
            if (ContinueWithError && string.IsNullOrEmpty(OriginalManifestPath))
            {
                Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_ManifestFile_NotExist);
                return false;
            }

            return true;
        }
    }
}
