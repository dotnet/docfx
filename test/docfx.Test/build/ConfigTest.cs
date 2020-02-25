// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class ConfigTest
    {
        [Theory]
        [InlineData(null, "zh-cn", null)]
        [InlineData("", "zh-cn", "")]
        [InlineData("https://github.com/docfx/name", "", "https://github.com/docfx/name")]
        [InlineData("https://github.com/docfx/name", null, "https://github.com/docfx/name")]
        [InlineData("https://github.com/docfx/name", "zh-cn", "https://github.com/docfx/name.zh-cn")]
        [InlineData("https://github.com/docfx/name.en-us", "zh-cn", "https://github.com/docfx/name.en-us.zh-cn")]
        [InlineData("https://github.com/docfx/name.zh-cn", "zh-cn", "https://github.com/docfx/name.zh-cn")]
        [InlineData("https://github.com/docfx/name.en", "zh-cn", "https://github.com/docfx/name.en.zh-cn")]
        [InlineData("https://github.com/docfx/test-repo", "en-us", "https://github.com/docfx/test-repo")]
        [InlineData("https://github.com/docfx/en-us", "zh-cn", "https://github.com/docfx/en-us.zh-cn")]
        [InlineData("https://github.com/docfx/test-repo", "bs-Cyrl-BA", "https://github.com/docfx/test-repo.bs-Cyrl-BA")]
        [InlineData("https://github.com/docfx/test-repo.en-us", "bs-Cyrl-BA", "https://github.com/docfx/test-repo.en-us.bs-Cyrl-BA")]
        [InlineData("https://github.com/docfx/test-repo.bs-Cyrl-BA", "sr-Latn-RS", "https://github.com/docfx/test-repo.bs-Cyrl-BA.sr-Latn-RS", "bs-Cyrl-BA")]
        [InlineData("https://test.visualstudio.com/_git/TripleCrown.Backend", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.sr-Latn-RS")]
        [InlineData("https://test.visualstudio.com/_git/TripleCrown.Backend.en-us", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.en-us.sr-Latn-RS")]
        [InlineData("https://test.visualstudio.com/_git/TripleCrown.Backend.sr-Latn-RS", "sr-Latn-RS", "https://test.visualstudio.com/_git/TripleCrown.Backend.sr-Latn-RS")]
        public static void LocConfigConventionRepoRemote(string sourceName, string locale, string locName, string defaultLocale = "en-us")
            => Assert.Equal(locName, LocalizationUtility.GetLocalizedRepo(false, sourceName, "master", locale, defaultLocale).remote);

        [Theory]
        [InlineData(true, "", "zh-cn", "")]
        [InlineData(true, null, "zh-cn", null)]
        [InlineData(false, "master", "zh-cn", "master")]
        [InlineData(false, "master", "en-us", "master")]
        [InlineData(true, "master", "zh-cn", "master-sxs")]
        public static void LocConfigConventionRepoBranch(bool enableBilingual, string sourceBranch, string locale, string targetBranch)
            => Assert.Equal(targetBranch, LocalizationUtility.GetLocalizedRepo(enableBilingual, "abc", sourceBranch, locale, "en-us").branch);

        [Theory]
        [InlineData("https://github.com/docs/theme", "en-us", "en-us", "https://github.com/docs/theme#master")]
        [InlineData("https://github.com/docs/theme", "zh-cn", "en-us", "https://github.com/docs/theme.zh-cn#master")]
        [InlineData("https://github.com/docs/theme", "", "en-us", "https://github.com/docs/theme#master")]
        [InlineData("https://github.com/docs/theme.zh-cn", "zh-cn", "en-us", "https://github.com/docs/theme.zh-cn#master")]
        [InlineData("https://github.com/docs/theme.en-us", "zh-cn", "en-us", "https://github.com/docs/theme.en-us.zh-cn#master")]
        [InlineData("https://github.com/docs/theme#live", "zh-cn", "en-us", "https://github.com/docs/theme.zh-cn#live")]
        [InlineData("https://github.com/docs/theme.en-us#live", "zh-cn", "en-us", "https://github.com/docs/theme.en-us.zh-cn#live")]
        [InlineData("https://github.com/docs/theme.zh-cn#live", "zh-cn", "en-us", "https://github.com/docs/theme.zh-cn#live")]
        public static void LocConfigConventionTheme(string theme, string locale, string defaultLocale, string expectedTheme)
        {
            var locTheme = LocalizationUtility.GetLocalizedTheme(new PackagePath(theme), locale, defaultLocale);
            Assert.Equal(expectedTheme, locTheme.ToString());
        }

        [Theory]
        [InlineData("https://github.com/docs", "master", null, null, null)]
        [InlineData("", "master", null, null, null)]
        [InlineData("", null, null, null, null)]
        [InlineData("https://github.com/docs.zh-cn", "master", "https://github.com/docs", "master", "zh-cn")]
        [InlineData("https://github.com/docs.zh-CN", "master", "https://github.com/docs", "master", "zh-cn")]
        [InlineData("https://github.com/docs.bs-Cyrl-BA", "master", "https://github.com/docs", "master", "bs-cyrl-ba")]
        [InlineData("https://test.visualstudio.com/_git/abc", "master", null, null, null)]
        [InlineData("https://test.visualstudio.com/_git/abc.zh-cn", "master", "https://test.visualstudio.com/_git/abc", "master", "zh-cn")]
        [InlineData("https://test.visualstudio.com/_git/abc.bs-Cyrl-BA", "master", "https://test.visualstudio.com/_git/abc", "master", "bs-cyrl-ba")]
        [InlineData("https://github.com/docs.zh-cn", "master-sxs", "https://github.com/docs", "master", "zh-cn")]
        [InlineData("https://github.com/docs.loc", "master-sxs.zh-cn", "https://github.com/docs", "master", "zh-cn")]
        public static void LocConfigConventionSourceRepo(string remote, string branch, string expectedSourceRemote, string expectedSourceBranch, string expectedLocale)
        {
            LocalizationUtility.TryGetFallbackRepository(remote, branch, out var sourceRemote, out var sourceBranch, out var locale);

            Assert.Equal(expectedSourceRemote, sourceRemote);
            Assert.Equal(expectedSourceBranch, sourceBranch);
            Assert.Equal(expectedLocale, locale);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("en", false)]
        [InlineData("zh", false)]
        [InlineData("id-id", true)]
        [InlineData("ms-my", true)]
        [InlineData("ca-es", true)]
        [InlineData("cs-cz", true)]
        [InlineData("da-dk", true)]
        [InlineData("de-AT", true)]
        [InlineData("de-CH", true)]
        [InlineData("de-de", true)]
        [InlineData("et-ee", true)]
        [InlineData("en-AU", true)]
        [InlineData("en-CA", true)]
        [InlineData("en-IN", true)]
        [InlineData("en-IE", true)]
        [InlineData("en-MY", true)]
        [InlineData("en-NZ", true)]
        [InlineData("en-SG", true)]
        [InlineData("en-ZA", true)]
        [InlineData("en-GB", true)]
        [InlineData("en-us", true)]
        [InlineData("es-MX", true)]
        [InlineData("es-es", true)]
        [InlineData("eu-es", true)]
        [InlineData("fil-ph", true)]
        [InlineData("fr-BE", true)]
        [InlineData("fr-CA", true)]
        [InlineData("fr-CH", true)]
        [InlineData("fr-fr", true)]
        [InlineData("ga-ie", true)]
        [InlineData("gl-es", true)]
        [InlineData("hr-hr", true)]
        [InlineData("is-is", true)]
        [InlineData("it-CH", true)]
        [InlineData("it-it", true)]
        [InlineData("lv-lv", true)]
        [InlineData("lb-lu", true)]
        [InlineData("lt-lt", true)]
        [InlineData("hu-hu", true)]
        [InlineData("mt-mt", true)]
        [InlineData("nl-BE", true)]
        [InlineData("nl-nl", true)]
        [InlineData("nb-NO", true)]
        [InlineData("pl-pl", true)]
        [InlineData("pt-BR", true)]
        [InlineData("pt-pt", true)]
        [InlineData("ro-ro", true)]
        [InlineData("sk-sk", true)]
        [InlineData("sl-si", true)]
        [InlineData("fi-fi", true)]
        [InlineData("sv-se", true)]
        [InlineData("vi-vn", true)]
        [InlineData("tr-tr", true)]
        [InlineData("el-gr", true)]
        [InlineData("bg-bg", true)]
        [InlineData("kk-kz", true)]
        [InlineData("ru-ru", true)]
        [InlineData("uk-ua", true)]
        [InlineData("he-il", true)]
        [InlineData("ar-sa", true)]
        [InlineData("hi-in", true)]
        [InlineData("th-th", true)]
        [InlineData("ko-kr", true)]
        [InlineData("ja-jp", true)]
        [InlineData("bs-cyrl-ba", true)]
        [InlineData("sr-cyrl-rs", true)]
        [InlineData("sr-latn-rs", true)]
        [InlineData("bs-latn-ba", true)]
        [InlineData("zh-CN", true)]
        [InlineData("zh-tw", true)]
        [InlineData("zh-hk", true)]
        public static void IsValidLocale(string locale, bool valid)
            => Assert.Equal(valid, LocalizationUtility.IsValidLocale(locale));

        [Theory]
        [InlineData("", "")]
        [InlineData("a", "a")]
        [InlineData("OUTPUT_PATH", "outputPath")]
        [InlineData("OUTPUT_PATH_NAME", "outputPathName")]
        public static void ToCamelCaseTest(string name, string camelCaseName)
        {
            Assert.Equal(camelCaseName, StringUtility.ToCamelCase('_', name));
        }

        [Theory]
        [InlineData("https://a.com/a", "a")]
        [InlineData("https://a.com/a/1", "a")]
        [InlineData("https://a.com/a/b", "a/b")]
        [InlineData("https://a.com/a/b/1", "a/b")]
        public static void HttpCredential_Respect_LongestMatch(string url, string value)
        {
            var config = JsonUtility.Deserialize<PreloadConfig>(@"{
    'http': {
        'https://a.com/a': { 'headers': { 'key': 'a' } },
        'https://a.com/a/b': { 'headers': { 'key': 'a/b' } }
    }
}".Replace('\'', '"'), null);

            var credentialProvider = config.GetCredentialProvider();

            var message = new HttpRequestMessage { RequestUri = new Uri(url) };
            credentialProvider(message);
            Assert.Equal(value, message.Headers.GetValues("key").First());
        }
    }
}
