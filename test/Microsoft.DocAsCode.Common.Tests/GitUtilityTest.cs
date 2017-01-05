// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Xunit;
    using YamlDotNet.Core;

    using Microsoft.DocAsCode.Common.Git;
    using Microsoft.DocAsCode.YamlSerialization;

    [Trait("Owner", "makaretu")]
    public class GitUtilityTest
    {
        [Fact]
        public void Environment_ForBranchName()
        {
            const string envName = "Git_Branch";
            var original = Environment.GetEnvironmentVariable(envName);
            try
            {
                Environment.SetEnvironmentVariable(envName, "special-branch");
                var info = GitUtility.GetFileDetail(Directory.GetCurrentDirectory());
                Assert.Equal("special-branch", info.RemoteBranch);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envName, original);
            }
        }

    }
}
