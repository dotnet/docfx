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
        [InlineData(null, "zh-cn", null)]
        [InlineData("", "zh-cn", "")]
        [InlineData("github/name", "", "github/name")]
        [InlineData("github/name", null, "github/name")]
        [InlineData("github/name", "zh-cn", "github/name.zh-cn")]
        [InlineData("github/name.en-us", "zh-cn", "github/name.zh-cn")]
        [InlineData("github/name.en-US", "zh-cn", "github/name.zh-cn")]
        [InlineData("github/name.en-US", "zh-CN", "github/name.zh-CN")]
        [InlineData("github/name.en", "zh-cn", "github/name.en.zh-cn")]
        [InlineData("github/name.en-us", "en-us", "github/name.en-us")]
        [InlineData("github/test-repo", "en-us", "github/test-repo")]
        [InlineData("github/en-us", "zh-cn", "github/en-us.zh-cn")]
        public static void LocConfigConversionEditRepoName(string sourceName, string locale, string locName)
            => Assert.Equal(locName, LocConfigConversion.GetEditRepository(sourceName, locale, "en-us"));
    }
}
