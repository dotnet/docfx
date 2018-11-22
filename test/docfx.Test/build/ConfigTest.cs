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
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/test-repo.bs-Cyrl-BA", "sr-Latn-RS", "https://github.com/docfx/test-repo.sr-Latn-RS", "bs-Cyrl-BA")]
        [InlineData(LocalizationMapping.Repository, "https://github.com/docfx/test-repo.bs-Cyrl-BA", "sr-Latn-RS", "https://github.com/docfx/test-repo.bs-Cyrl-BA.sr-Latn-RS")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://github.com/docfx/test-repo", "zh-cn", "https://github.com/docfx/test-repo.localization")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://github.com/docfx/test-repo.en-us", "zh-cn", "https://github.com/docfx/test-repo.localization")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://github.com/docfx/test-repo.bs-Cyrl-BA", "sr-Latn-RS", "https://github.com/docfx/test-repo.localization", "bs-Cyrl-BA")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://github.com/docfx/test-repo.bs-Cyrl-BA", "sr-Latn-RS", "https://github.com/docfx/test-repo.bs-Cyrl-BA.localization")]
        [InlineData(LocalizationMapping.Repository, "https://test.visualstudio.com/_git/TripleCrown.Backend", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.sr-Latn-RS")]
        [InlineData(LocalizationMapping.Repository, "https://test.visualstudio.com/_git/TripleCrown.Backend.en-us", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.sr-Latn-RS")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://test.visualstudio.com/_git/TripleCrown.Backend", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.localization")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://test.visualstudio.com/_git/TripleCrown.Backend.zh-cn", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.localization", "zh-cn")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, "https://test.visualstudio.com/_git/TripleCrown.Backend.zh-cn", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.zh-cn.localization")]
        public static void LocConfigConventionRepoRemote(LocalizationMapping locMappingType, string sourceName, string locale, string locName, string defaultLocale = "en-us")
            => Assert.Equal(locName, LocalizationConvention.GetLocalizationRepo(locMappingType, false, sourceName, "master", locale, defaultLocale).remote);

        [Theory]
        [InlineData(LocalizationMapping.Folder, true, "master", "zh-cn", "master")]
        [InlineData(LocalizationMapping.Repository, true, "", "zh-cn", "")]
        [InlineData(LocalizationMapping.Repository, true, null, "zh-cn", null)]
        [InlineData(LocalizationMapping.Repository, false, "master", "zh-cn", "master")]
        [InlineData(LocalizationMapping.Repository, false, "master", "en-us", "master")]
        [InlineData(LocalizationMapping.Repository, true, "master", "zh-cn", "master-sxs")]
        [InlineData(LocalizationMapping.RepositoryAndFolder, true, "master", "zh-cn", "master-sxs")]
        public static void LocConfigConventionRepoBranch(LocalizationMapping locMappingType, bool enableBilingual, string sourceBranch, string locale, string targetBranch)
            => Assert.Equal(targetBranch, LocalizationConvention.GetLocalizationRepo(locMappingType, enableBilingual, "abc", sourceBranch, locale, "en-us").branch);

        [Theory]
        [InlineData("https://github.com/docs/theme", "en-us", "en-us", "https://github.com/docs/theme")]
        [InlineData("https://github.com/docs/theme", "zh-cn", "en-us", "https://github.com/docs/theme")]
        [InlineData("https://github.com/docs/theme", "", "en-us", "https://github.com/docs/theme.#master")]
        [InlineData("https://github.com/docs/theme.zh-cn", "zh-cn", "en-us", "https://github.com/docs/theme.zh-cn")]
        [InlineData("https://github.com/docs/theme.en-us", "zh-cn", "en-us", "https://github.com/docs/theme.zh-cn#master")]
        [InlineData("", "zh-cn", "en-us", "")]
        public static void LocConfigConventionTheme(string theme, string locale, string defaultLocale, string expectedTheme)
            => Assert.Equal(expectedTheme, LocalizationConvention.GetLocalizationTheme(theme, locale, defaultLocale));
    }
}
