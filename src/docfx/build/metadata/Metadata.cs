// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Metadata
    {
        public static JObject GetCommon(Document file)
        {
            Debug.Assert(file != null);

            return JsonUtility.Merge(
                GetGitRelated(file),
                GetFromConfig(file));
        }

        private static JObject GetFromConfig(Document file)
        {
            var config = file.Docset.Config;
            var fileMetadata =
                from item in config.FileMetadata
                where item.Match(file.FilePath)
                select item.Value;

            return JsonUtility.Merge(config.GlobalMetadata, fileMetadata);
        }

        private static JObject GetGitRelated(Document file)
        {
            var result = new JObject();
            var repo = GitRepoInfoProvider.GetGitRepoInfo(file);
            if (repo == null)
                return result;

            var fullPath = Path.GetFullPath(Path.Combine(file.Docset.DocsetPath, file.FilePath));
            var relPath = PathUtility.NormalizeFile(Path.GetRelativePath(repo.RootPath, fullPath));
            result[MetadataConstants.GitCommit] = GitUtility.GetGitPermaLink(repo, relPath);
            result[MetadataConstants.OriginalContentGitUrl] = GitUtility.GetGitLink(repo, relPath);
            return result;
        }
    }
}
