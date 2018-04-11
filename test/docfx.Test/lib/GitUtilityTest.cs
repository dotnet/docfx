// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Docs.Test
{
    public static class GitUtilityTest
    {
        [Theory]
        [InlineData("README.md")]
        public static async Task GetCommitsSameAsGitLog(string file)
        {
            var repo = GitUtility.FindRepo(Path.GetFullPath(file));
            var pathToRepo = PathUtility.NormalizeFile(file);
            var exe = await GitUtility.GetCommits(repo, pathToRepo);
            var lib = GitUtility.GetCommits(repo, new List<string> { pathToRepo })[0].ToList();
            Assert.Equal(JsonConvert.SerializeObject(exe), JsonConvert.SerializeObject(lib));
        }

        [Fact]
        public static async Task GitCommandConcurreny()
        {
            var cwd = GitUtility.FindRepo(Path.GetFullPath("README.md"));

            var results = await Task.WhenAll(Enumerable.Range(0, 10).AsParallel().Select(i => GitUtility.HeadRevision(cwd)));

            Assert.True(results.All(r => r.Any()));
        }
    }
}
