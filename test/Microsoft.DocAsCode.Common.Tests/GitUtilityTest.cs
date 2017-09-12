// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;
    using System.IO;

    using Xunit;

    using Microsoft.DocAsCode.Common.Git;

    [Collection("docfx STA")]
    [Trait("Owner", "makaretu")]
    public class GitUtilityTest : IDisposable
    {
        private string _originalBranchName;
        private const string envName = "DOCFX_SOURCE_BRANCH_NAME";
        public GitUtilityTest()
        {
            _originalBranchName = Environment.GetEnvironmentVariable(envName);
            Environment.SetEnvironmentVariable(envName, "special-branch");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(envName, _originalBranchName);
        }

        [Fact]
        public void Environment_ForBranchName()
        {

            var info = GitUtility.GetFileDetail(Directory.GetCurrentDirectory());
            Assert.Equal("special-branch", info.RemoteBranch);
        }

        [Fact]
        public void GetDeletedFile()
        {
            var repoInfo = GitUtility.GetRepoInfo(Directory.GetCurrentDirectory());
            var deletedExistingFile = Path.Combine(repoInfo.RepoRootPath, @"src/docfx.website.themes/angular/README.md");
            var deletedNotExistingFile = Path.Combine(repoInfo.RepoRootPath, @"NOTEXISTING.md");

            var content = GitUtility.GetDeletedFileContent(deletedExistingFile);
            Assert.NotNull(content);

            content = GitUtility.GetDeletedFileContent(deletedNotExistingFile);
            Assert.Null(content);
        }
    }
}
