// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal interface IGitHubClient
    {
        Task<(Error error, GitHubUser user)> GetUserByLogin(SourceInfo<string> login);

        Task<(Error error, GitHubUser user)> GetUserByEmail(string email);

        Task<(Error error, GitHubUser user)> GetUserByCommit(string email, string owner, string name, string commit);
    }
}
