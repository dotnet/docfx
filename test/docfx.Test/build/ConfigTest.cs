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
        [InlineData(LocalizationMapping.Repository, null, "zh-cn", null)]
        [InlineData(LocalizationMapping.Repository, "", "zh-cn", "")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/name", "", "https://github.com/docfx/name")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/name", null, "https://github.com/docfx/name")]
        [InlineData(LocalizationMapping.Folder, "https://github.com/docfx/name", "zh-cn", "https://github.com/docfx/name")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/name", "zh-cn", "https://github.com/docfx/name.zh-cn")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/name.en-us", "zh-cn", "https://github.com/docfx/name.zh-cn")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/name.en-US", "zh-cn", "https://github.com/docfx/name.zh-cn")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/name.en-US", "zh-CN", "https://github.com/docfx/name.zh-CN")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/name.en", "zh-cn", "https://github.com/docfx/name.en.zh-cn")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/name.en-us", "en-us", "https://github.com/docfx/name.en-us")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/test-repo", "en-us", "https://github.com/docfx/test-repo")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/en-us", "zh-cn", "https://github.com/docfx/en-us.zh-cn")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/test-repo", "bs-Cyrl-BA", "https://github.com/docfx/test-repo.bs-Cyrl-BA")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/test-repo.en-us", "bs-Cyrl-BA", "https://github.com/docfx/test-repo.bs-Cyrl-BA")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/test-repo.bs-Cyrl-BA", "sr-Latn-RS", "https://github.com/docfx/test-repo.sr-Latn-RS")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://github.com/docfx/test-repo", "zh-cn", "https://github.com/docfx/test-repo.localization")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://github.com/docfx/test-repo.en-us", "zh-cn", "https://github.com/docfx/test-repo.localization")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://github.com/docfx/test-repo.bs-Cyrl-BA", "sr-Latn-RS", "https://github.com/docfx/test-repo.localization")]
        [InlineData(LocalizationMapping.Repository, "https://test.visualstudio.com/_git/TripleCrown.Backend", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.sr-Latn-RS")]
        [InlineData(LocalizationMapping.Repository, "https://test.visualstudio.com/_git/TripleCrown.Backend.zh-cn", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.sr-Latn-RS")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://test.visualstudio.com/_git/TripleCrown.Backend", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.localization")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://test.visualstudio.com/_git/TripleCrown.Backend.zh-cn", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.localization")]
        public static void LocConfigConventionEditRepoName(LocalizationMapping locMappingType, string sourceName, string locale, string locName)
            => Assert.Equal(locName, LocalizationConvention.GetLocalizationRepo(locMappingType, sourceName, locale, "en-us").remote);
    }
}
