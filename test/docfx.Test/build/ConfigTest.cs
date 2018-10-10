// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public static class ConfigTest
    {
        [Theory]
        [InlineData("locales: [zh-cn]", true, new string[0], new[] { "zh-cn" })]
        [InlineData("locales: [ zh-cn, de-de ]", true, new string[0], new[] { "zh-cn", "de-de" })]
        [InlineData("branches:   [master]", true, new[] { "master" }, new string[0])]
        [InlineData("branches:   [master,   live]", true, new[] { "master", "live" }, new string[0])]
        [InlineData("branches: [master, live] locales: [zh-cn]", true, new[] { "master", "live" }, new[] { "zh-cn" })]
        [InlineData("branches: [master, live]     locales: [zh-cn, de-de]", true, new[] { "master", "live" }, new[] { "zh-cn", "de-de" })]
        [InlineData("branches: [live, live]     locales: [zh-cn, de-de]", true, new[] { "live" }, new[] { "zh-cn", "de-de" })]
        [InlineData("branches: [LIVE, live]     locales: [zh-cn, ZH-CN]", true, new[] { "LIVE", "live" }, new[] { "zh-cn" })]
        [InlineData("locales: zh-cn branches: [live]", false, null, null)]
        [InlineData("branches: [live] locales: zh-cn", false, null, null)]
        [InlineData("branches: live", false, null, null)]
        [InlineData("locales: zh-cn", false, null, null)]
        public static void OverwriteConifgIdentifierMatch(string str, bool matched, string[] matchedBranches, string[] matchedLocales)
        {
            Assert.Equal(matched, OverwriteConfigIdentifier.TryMatch(str, out var identifier));
            if (matched)
            {
                Assert.Equal(matchedBranches, identifier.Branches);
                Assert.Equal(matchedLocales, identifier.Locales);
            }
        }

        [Theory]
        [InlineData(LocMappingType.Repository, "", null, "zh-cn", null)]
        [InlineData(LocMappingType.Repository, "", "", "zh-cn", "")]
        [InlineData(LocMappingType.Folder, "", "", "zh-cn", "")]
        [InlineData(LocMappingType.Repository, "github", "name", "", "name")]
        [InlineData(LocMappingType.Repository, "", "name", "zh-cn", "name.zh-cn")]
        [InlineData(LocMappingType.Repository, null, "name", "zh-cn", "name.zh-cn")]
        [InlineData(LocMappingType.Repository, "github", "name", null, "name")]
        [InlineData(LocMappingType.Repository, "github", "name", "zh-cn", "name.zh-cn")]
        [InlineData(LocMappingType.Repository, "github", "name.en-us", "zh-cn", "name.zh-cn")]
        [InlineData(LocMappingType.Repository, "github", "name.en-US", "zh-cn", "name.zh-cn")]
        [InlineData(LocMappingType.Repository, "github", "name.en-US", "zh-CN", "name.zh-CN")]
        [InlineData(LocMappingType.Repository, "github", "name.en", "zh-cn", "name.en.zh-cn")]
        [InlineData(LocMappingType.Repository, "github", "name.en-us", "en-us", "name.en-us")]
        [InlineData(LocMappingType.Repository, "github", "test-repo", "en-us", "test-repo")]
        [InlineData(LocMappingType.Repository, "github", "en-us", "zh-cn", "en-us.zh-cn")]
        [InlineData(LocMappingType.Repository, "github", "test-repo", "bs-Cyrl-BA", "test-repo.bs-Cyrl-BA")]
        [InlineData(LocMappingType.Repository, "github", "test-repo.en-us", "bs-Cyrl-BA", "test-repo.bs-Cyrl-BA")]
        [InlineData(LocMappingType.Repository, "github", "test-repo.bs-Cyrl-BA", "sr-Latn-RS", "test-repo.sr-Latn-RS")]
        [InlineData(LocMappingType.RepositoryAndFolder, "github", "test-repo", "zh-cn", "test-repo.localization")]
        [InlineData(LocMappingType.RepositoryAndFolder, "github", "test-repo.en-us", "zh-cn", "test-repo.localization")]
        [InlineData(LocMappingType.RepositoryAndFolder, "github", "test-repo.bs-Cyrl-BA", "sr-Latn-RS", "test-repo.localization")]
        public static void LocConfigConventionEditRepoName(LocMappingType locMappingType, string sourceOwner, string sourceName, string locale, string locName, string locOwner = null)
            => Assert.Equal((locOwner ?? sourceOwner, locName), LocConfigConvention.GetLocRepository(locMappingType, sourceOwner, sourceName, locale, "en-us"));
    }
}
