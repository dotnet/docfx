// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal interface IRestoreMap
    {
        string ReadFile(SourceInfo<string> url);

        string GetFilePath(SourceInfo<string> url);

        (string path, string commit) GetGitRepositoryPath(string url, string branch);

        bool TryGetGitRepositoryPath(string url, string branch, out string path, out string commit);
    }
}
