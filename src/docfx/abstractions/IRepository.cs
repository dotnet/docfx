// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal interface IRepository
    {
        byte[] ReadBytes(string path, string committish = null);

        string[] ListFilesRecursive(string committish = null);

        GitCommit[] GetCommitHistory(string committish = null);

        GitCommit[] GetFileCommitHistory(string path, string committish = null);
    }
}
