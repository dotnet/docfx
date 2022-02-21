// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Docs.Build;

public static class ConfigTest
{
    [Theory]
    [InlineData("DOCFX_FOO", "bar", "{'foo':'bar'}")]
    [InlineData("DOCFX_FOO", "a;b;c", "{'foo':['a','b','c']}")]
    [InlineData("DOCFX__SECRETS__GITHUB_TOKEN", "a", "{'secrets':{'githubToken':'a'}}")]
    [InlineData("DOCFX_SECRETS", "{\"githubToken\":\"a\"}", "{'secrets':{'githubToken':'a'}}")]
    public static void LoadConfigFromEnvironmentVariable(string name, string value, string expected)
    {
        var actual = ConfigLoader.LoadEnvironmentVariables(new[] { new DictionaryEntry(name, value) });
        Assert.Equal(expected, actual.ToString(Formatting.None).Replace('\"', '\''));
    }

    [Theory]
    [InlineData("https://github.com/docs", "master", null, null)]
    [InlineData("", "master", null, null)]
    [InlineData("", null, null, null)]
    [InlineData("https://github.com/docs.zh-cn", "master", "https://github.com/docs", "master")]
    [InlineData("https://github.com/docs.zh-CN", "master", "https://github.com/docs", "master")]
    [InlineData("https://github.com/docs.bs-Cyrl-BA", "master", "https://github.com/docs", "master")]
    [InlineData("https://test.visualstudio.com/_git/abc", "master", null, null)]
    [InlineData("https://test.visualstudio.com/_git/abc.zh-cn", "master", "https://test.visualstudio.com/_git/abc", "master")]
    [InlineData("https://test.visualstudio.com/_git/abc.bs-Cyrl-BA", "master", "https://test.visualstudio.com/_git/abc", "master")]
    public static void LocConfigConventionSourceRepo(string remote, string branch, string expectedSourceRemote, string expectedSourceBranch)
    {
        var (sourceRemote, sourceBranch) = LocalizationUtility.GetFallbackRepository(remote, branch);

        Assert.Equal(expectedSourceRemote, sourceRemote);
        Assert.Equal(expectedSourceBranch, sourceBranch);
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
    [InlineData("https://a.com/a", "a")]
    [InlineData("https://a.com/a/1", "a")]
    [InlineData("https://a.com/a/b", "a/b")]
    [InlineData("https://a.com/a/b/1", "a/b")]
    public static void HttpCredential_Respect_LongestMatch(string url, string value)
    {
        var secrets = JsonUtility.DeserializeData<SecretConfig>(
            @"{
    'http': {
        'https://a.com/a': { 'headers': { 'key': 'a' } },
        'https://a.com/a/b': { 'headers': { 'key': 'a/b' } }
    }
}".Replace('\'', '"'), null);

        var httpConfig = secrets.GetHttpConfig(url);

        Assert.NotNull(httpConfig);
        Assert.Equal(value, httpConfig.Headers["key"]);
    }
}
