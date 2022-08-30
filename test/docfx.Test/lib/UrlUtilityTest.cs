// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build;

public static class UrlUtilityTest
{
    [Theory]
    [InlineData("", "", "", "")]
    [InlineData("a", "a", "", "")]
    [InlineData("a#b", "a", "", "#b")]
    [InlineData("a#b<", "a", "", "#b<")]
    [InlineData("a?c", "a", "?c", "")]
    [InlineData("a?b#c", "a", "?b", "#c")]
    [InlineData("a#b?c=d", "a", "", "#b?c=d")]
    [InlineData("a?b#c?d=e", "a", "?b", "#c?d=e")]
    [InlineData("a?b#c#d", "a", "?b", "#c#d")]
    public static void SplitUrl(string url, string path, string query, string fragment)
    {
        var (aPath, aQuery, aFragment) = UrlUtility.SplitUrl(url);

        Assert.Equal(path, aPath);
        Assert.Equal(query, aQuery);
        Assert.Equal(fragment, aFragment);
    }

    [Theory]
    [InlineData("", "", "", "")]
    [InlineData("", "?b", "#c", "?b#c")]
    [InlineData("a", "?b=1", "#c", "a?b=1#c")]
    [InlineData("a", "", null, "a")]
    [InlineData("a?b=1#c", "?b=2", "#c1", "a?b=2#c1")]
    [InlineData("a?b=1#c", "?b1=1", "", "a?b=1&b1=1#c")]
    [InlineData("a?b=1#c", "", "#c1", "a?b=1#c1")]
    [InlineData("", "?", "#c1", "?#c1")]
    public static void MergeUrl(string url, string query, string fragment, string expected)
    {
        var result = UrlUtility.MergeUrl(url, query, fragment);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(@"", LinkType.RelativePath)]
    [InlineData(@"a", LinkType.RelativePath)]
    [InlineData(@"a/b", LinkType.RelativePath)]
    [InlineData(@"a\b", LinkType.RelativePath)]
    [InlineData(@"/", LinkType.AbsolutePath)]
    [InlineData(@"/a", LinkType.AbsolutePath)]
    [InlineData(@"\\a", LinkType.External)]
    [InlineData(@"//a", LinkType.External)]
    [InlineData(@"#", LinkType.SelfBookmark)]
    [InlineData(@"#a", LinkType.SelfBookmark)]
    [InlineData(@"http://a", LinkType.External)]
    [InlineData(@"https://a.com", LinkType.External)]
    [InlineData(@"http:a", LinkType.RelativePath)]
    [InlineData(@"feedback-url:?query=a", LinkType.External)]
    [InlineData(@"c:/a", LinkType.WindowsAbsolutePath)]
    [InlineData(@"c:\a", LinkType.WindowsAbsolutePath)]
    [InlineData(@"file://a.md", LinkType.External)]
    public static void GetLinkType(string url, LinkType expected)
    {
        Assert.Equal(expected, UrlUtility.GetLinkType(url));
    }

    [Theory]
    [InlineData("/", "", "")]
    [InlineData("/", "https://github.com", "https://github.com")]
    [InlineData("/", "a", "a")]
    [InlineData("/", "/", "./")]
    [InlineData("/a", "/b", "b")]
    [InlineData("/a/b", "/b/c", "../b/c")]
    [InlineData("/a", "/a/", "a/")]
    [InlineData("/a/b", "/a/", "./")]
    [InlineData("/a/", "/a", "../a")]
    [InlineData("/a/b/", "/a", "../../a")]
    [InlineData("/a/b/c/", "/a", "../../../a")]
    [InlineData("/a/", "/a/", "./")]
    [InlineData("/a/", "/b", "../b")]
    [InlineData("/", "/a", "a")]
    [InlineData("/ab", "/a", "a")]
    [InlineData("/a", "/", "./")]
    [InlineData("/a", "/#bookmark", "./#bookmark")]
    [InlineData("/a", "/?query", "./?query")]
    [InlineData("/a#bookmark", "/", "./")]
    [InlineData("/a?query", "/", "./")]
    [InlineData("/a/", "/", "../")]
    public static void GetRelativeUrl(string relativeToUrl, string url, string expected)
    {
        Assert.Equal(expected, UrlUtility.GetRelativeUrl(relativeToUrl, url));
    }

    [Theory]
    [InlineData("a", false)]
    [InlineData("a/b", false)]
    [InlineData("a\\b", false)]
    [InlineData("/a", false)]
    [InlineData("\\a", false)]
    [InlineData("#a", false)]
    [InlineData("c:/a", false)]
    [InlineData("c:\\a", false)]
    [InlineData("http://a", true)]
    [InlineData("http://////a", false)]
    [InlineData("https://a.com", true)]
    [InlineData("https://a.com#b", true)]
    [InlineData("https://////a.com", false)]
    public static void IsHttp(string url, bool expected)
        => Assert.Equal(expected, UrlUtility.IsHttp(url));

    [Theory]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb924")]
    [InlineData("https://github.com/dotnet/docfx", "github.com+dotnet+docfx+5fa6f8cdf466438b475e5aa429078cf8")]
    [InlineData("https://github.com/1/2/3/4/5/6/7/8/9/10/11/12/13/14", "github.com+1+2+3+11+12+13+14+95662b4142069fc944aa91531bbaf0f2")]
    [InlineData("https://github.com/crazy-crazy-crazy-crazy-long-repo.zh-cn", "github.com+crazy-cr..po.zh-cn+574d9cf4127558addef3080fd7011891")]
    [InlineData("https://a.com?b=c#d", "a.com+b=c+d+38f7bfa5bdfcfc843d87c657f9886d62")]
    [InlineData("https://ab-c.blob.core.windows.net/a/b/c/d?sv=d&sr=e&sig=f&st=2019-05-07&se=2019-05-08&sp=r", "ab-c.blo..dows.net+a+b+c+d+d8b987862ffce09ceef9c9aaceaa9440")]
    public static void UrlToFolderName(string url, string folderName)
    {
        var result = UrlUtility.UrlToShortName(url);
        Assert.Equal(folderName, result);
    }

    [Theory]
    [InlineData("http://github.com/", false, null, null)]
    [InlineData("http://github.com/org", false, null, null)]
    [InlineData("http://github.com/org/name/unknown", false, null, null)]
    [InlineData("http://github.com/org/name#", false, null, null)]
    [InlineData("http://github.com/org/name/", true, "org", "name")]
    [InlineData("http://github.com/org/name", true, "org", "name")]
    [InlineData("http://github.com/org/name#branch", true, "org", "name")]
    [InlineData("https://github.com/org/name#branch", true, "org", "name")]
    public static void ParseGithubUrl(string remote, bool parsed, string expectedOwner, string expectedName)
    {
        if (UrlUtility.TryParseGitHubUrl(remote, out var owner, out var name))
        {
            Assert.True(parsed);
            Assert.Equal(expectedOwner, owner);
            Assert.Equal(expectedName, name);
            return;
        }

        Assert.False(parsed);
    }

    [Theory]
    [InlineData("https://ceapex.visualstudio.com/", false, null, null, null)]
    [InlineData("http://ceapex.visualstudio.com/project", false, null, null, null)]
    [InlineData("http://ceapex.visualstudio.com/project/_git", false, null, null, null)]
    [InlineData("http://ceapex.visualstudio.com/project/repo", false, null, null, null)]
    [InlineData("http://ceapex.visualstudio.com/project/_git/repo/unknown", false, null, null, null)]
    [InlineData("http://ceapex.visualstudio.com/project/_git/repo#", false, null, null, null)]
    [InlineData("https://dev.azure.com/ceapex", false, null, null, null)]
    [InlineData("http://dev.azure.com/ceapex/project", false, null, null, null)]
    [InlineData("http://dev.azure.com/ceapex/project/_git", false, null, null, null)]
    [InlineData("http://dev.azure.com/ceapex/project/repo", false, null, null, null)]
    [InlineData("http://dev.azure.com/ceapex/project/_git/repo/unknown", false, null, null, null)]
    [InlineData("http://dev.azure.com/ceapex/project/_git/repo#", false, null, null, null)]
    [InlineData("http://ceapex.visualstudio.com/project/_git/repo/", true, "project", "repo", "ceapex")]
    [InlineData("https://ceapex.visualstudio.com/project/_git/repo", true, "project", "repo", "ceapex")]
    [InlineData("http://ceapex.visualstudio.com/project/_git/repo", true, "project", "repo", "ceapex")]
    [InlineData("http://ceapex.visualstudio.com/project/_git/repo#branch", true, "project", "repo", "ceapex")]
    [InlineData("https://ceapex.visualstudio.com/project/_git/repo#branch", true, "project", "repo", "ceapex")]
    [InlineData("https://ceapex.visualstudio.com/DefaultCollection/project/_git/repo", true, "project", "repo", "ceapex")]
    [InlineData("http://ceapex.visualstudio.com/DefaultCollection/project/_git/repo/", true, "project", "repo", "ceapex")]
    [InlineData("https://ceapex.visualstudio.com/DefaultCollection/project/_git/repo#branch", true, "project", "repo", "ceapex")]
    [InlineData("http://ceapex.visualstudio.com/DefaultCollection/project/_git/repo#branch", true, "project", "repo", "ceapex")]
    [InlineData("https://dev.azure.com/ceapex/project/_git/repo", true, "project", "repo", "ceapex")]
    [InlineData("http://dev.azure.com/ceapex/project/_git/repo/", true, "project", "repo", "ceapex")]
    [InlineData("https://dev.azure.com/ceapex/project/_git/repo#branch", true, "project", "repo", "ceapex")]
    [InlineData("http://dev.azure.com/ceapex/project/_git/repo#branch", true, "project", "repo", "ceapex")]
    public static void ParseAzureReposUrl(string remote, bool parsed, string expectedProject, string expectedName, string expectedOrg)
    {
        if (UrlUtility.TryParseAzureReposUrl(remote, out var project, out var name, out var org))
        {
            Assert.True(parsed);
            Assert.Equal(expectedOrg, org);
            Assert.Equal(expectedProject, project);
            Assert.Equal(expectedName, name);
            return;
        }

        Assert.False(parsed);
    }

    [Theory]
    [InlineData("A.b[]", "a-b()")]
    [InlineData("a b", "a-b")]
    [InlineData("a\"b", "ab")]
    [InlineData("a%b", "ab")]
    [InlineData("a^b", "ab")]
    [InlineData("a\\b", "ab")]
    [InlineData("Dictionary<string, List<int>>*", "dictionary(string-list(int))*")]
    [InlineData("a'b'c", "abc")]
    [InlineData("{a|b_c'}", "((a-b-c))")]
    [InlineData("---&&$$##List<string> test(int a`, int a@, string b*)---&&$$##", "list(string)-test(int-a-int-a@-string-b*)")]
    [InlineData(
        "Microsoft.StreamProcessing.Streamable.AggregateByKey``4(Microsoft.StreamProcessing.IStreamable{Microsoft.StreamProcessing.Empty,``0},System.Linq.Expressions.Expression{System.Func{``0,``1}},Microsoft.StreamProcessing.Aggregates.IAggregate{``0,``22,``23}},Microsoft.StreamProcessing.Aggregates.IAggregate{``0,``30,``31}},System.Linq.Expressions.Expression{System.Func{``3,``5,``7,``9,``11,``13,``15,``17,``19,``21,``23,``25,``27,``29,``31,``32}})",
        "microsoft-streamprocessing-streamable-aggregatebykey-4(microsoft-streamprocessing-istreamable((microsoft-streamprocessing-empty-0))-system-linq-expressions-expression((system-func((-0-1))))-microsoft-streamprocessing-aggregates-iaggregate((-0-22-23))))-microsoft-streamprocessing-aggregates-iaggregate((-0-30-31))))-system-linq-expressions-expression((system-func((-3-5-7-9-11-13-15-17-19-21-23-25-27-29-31-32)))))")]
    public static void StandardizeBookmarks(string uid, string expectedBookmark)
    {
        var bookmark = UrlUtility.GetBookmark(uid);
        Assert.Equal(expectedBookmark, bookmark);
    }

    [Theory]
    [InlineData("docs.com/en-us/c", "docs.com", true, "docs.com/en-us/c")]
    [InlineData("https://docs.com/en-us/c", "docs.com", true, "/c")]
    [InlineData("https://docs.com/en-us/c", "docs.com", false, "/en-us/c")]
    [InlineData("https://docs.com/en-us/c", "", true, "https://docs.com/en-us/c")]
    [InlineData("https://docs.com/c", "docs.com", false, "/c")]
    [InlineData("https://docs.com/en-us/c", "docs1.com", true, "https://docs.com/en-us/c")]
    [InlineData("https://docs.com/", "docs.com", true, "/")]
    public static void RemoveHostName(string url, string hostName, bool removeLocale, string expected)
    {
        var result = UrlUtility.RemoveLeadingHostName(url, hostName, removeLocale);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/abc123-._~/def", "/abc123-._~/def")]
    [InlineData("/en-us/-._~!$&'()*+,;=:@", "/en-us/-._~!$&'()*+,;=:@")]
    [InlineData("/en-us/%[]", "/en-us/%25%5B%5D")]
    public static void EscapeUrlPathTest(string path, string expected)
    {
        var result = UrlUtility.EscapeUrlPath(path);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("abc123-._~def", "abc123-._~def")]
    [InlineData("-._~!$&'()*+,;=:@/?", "-._~!$&'()*+,;=:@/?")]
    [InlineData("%[]", "%25%5B%5D")]
    public static void EscapeUrlQueryTest(string queryOrFragment, string expected)
    {
        var result = UrlUtility.EscapeUrlQueryOrFragment(queryOrFragment);
        Assert.Equal(expected, result);
    }
}
