// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal interface IRepositoryProvider
    {
        bool TryGetRepositoryInfo(FilePath file, out string url, out string branch, out string commit);

        bool TryGetRepositoryInfo(FileOrigin origin, out string url, out string branch, out string commit);

        bool TryGetRepository(FilePath file, out IRepository repository);

        (string contentGitUrl, string originalContentGitUrl, string originalContentGitUrlTemplate, string gitCommit)
            GetGitUrls(FilePath file);
    }
}
