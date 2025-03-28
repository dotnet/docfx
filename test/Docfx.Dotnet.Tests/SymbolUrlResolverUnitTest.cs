// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Docfx.Dotnet.Tests;

[TestClass]
public class SymbolUrlResolverUnitTest
{
    [TestMethod]
    public void GetMicrosoftLearnUrlFromCommentIdTest()
    {
        var failures = new List<(string, Uri, Uri[])>();

        // The xrefmap for https://github.com/dotnet/dotnet-api-docs
        // downloaded and processed from https://learn.microsoft.com/en-us/dotnet/.xrefmap.json
        foreach (var line in File.ReadAllLines("TestData/dotnet-xrefmap.txt"))
        {
            var split = line.Split('\t');
            var commentId = split[0];
            var expectedUrl = new Uri($"https://learn.microsoft.com/dotnet/api/{split[1]}");

            var actualUrls = commentId[0] switch
            {
                // Enum fields and ordinary fields have different URL schemas
                'F' => new[]
                {
                    new Uri(SymbolUrlResolver.GetMicrosoftLearnUrl(commentId, isEnumMember: false, hasOverloads: false)),
                    new Uri(SymbolUrlResolver.GetMicrosoftLearnUrl(commentId, isEnumMember: true, hasOverloads: false)),
                },

                // Overload methods and ordinary methods have different URL schemas
                'M' =>
                [
                    new Uri(SymbolUrlResolver.GetMicrosoftLearnUrl(commentId, isEnumMember: false, hasOverloads: false)),
                    new Uri(SymbolUrlResolver.GetMicrosoftLearnUrl(commentId, isEnumMember: false, hasOverloads: true)),
                ],

                _ =>
                [
                    new Uri(SymbolUrlResolver.GetMicrosoftLearnUrl(commentId, isEnumMember: false, hasOverloads: false)),
                ],
            };

            if (!actualUrls.Contains(expectedUrl))
            {
                failures.Add((commentId, expectedUrl, actualUrls));
            }
        }

        // Ignore these remaining edge cases:
        //
        // - When UID differs only by case, mslearn appends _1 to disambiguate.
        // - When > 180 chars, type name is used over fully-qualified names to reduce URL length.
        Assert.AreEqual(56, failures.Count);
    }

    [TestMethod]
    [DataRow("A.b[]", "a-b()")]
    [DataRow("a b", "a-b")]
    [DataRow("a\"b", "ab")]
    [DataRow("a%b", "ab")]
    [DataRow("a^b", "ab")]
    [DataRow("a\\b", "ab")]
    [DataRow("Dictionary<string, List<int>>*", "dictionary(string-list(int))*")]
    [DataRow("a'b'c", "abc")]
    [DataRow("{a|b_c'}", "((a-b-c))")]
    [DataRow("---&&$$##List<string> test(int a`, int a@, string b*)---&&$$##", "list(string)-test(int-a-int-a@-string-b*)")]
    [DataRow(
        "Microsoft.StreamProcessing.Streamable.AggregateByKey``4(Microsoft.StreamProcessing.IStreamable{Microsoft.StreamProcessing.Empty,``0},System.Linq.Expressions.Expression{System.Func{``0,``1}},Microsoft.StreamProcessing.Aggregates.IAggregate{``0,``22,``23}},Microsoft.StreamProcessing.Aggregates.IAggregate{``0,``30,``31}},System.Linq.Expressions.Expression{System.Func{``3,``5,``7,``9,``11,``13,``15,``17,``19,``21,``23,``25,``27,``29,``31,``32}})",
        "microsoft-streamprocessing-streamable-aggregatebykey-4(microsoft-streamprocessing-istreamable((microsoft-streamprocessing-empty-0))-system-linq-expressions-expression((system-func((-0-1))))-microsoft-streamprocessing-aggregates-iaggregate((-0-22-23))))-microsoft-streamprocessing-aggregates-iaggregate((-0-30-31))))-system-linq-expressions-expression((system-func((-3-5-7-9-11-13-15-17-19-21-23-25-27-29-31-32)))))")]
    public void GetUrlFragmentFromUidTest(string uid, string expectedFragment)
    {
        Assert.AreEqual(expectedFragment, SymbolUrlResolver.GetUrlFragmentFromUid(uid));
    }

    [TestMethod]
    public void GetPdbSourceLinkUrlTest()
    {
        var (compilation, assembly) = CompilationHelper.CreateCompilationFromAssembly($"{typeof(DotnetApiCatalog).Assembly.GetName().Name}.dll");

        var type = assembly.GetTypeByMetadataName(typeof(DotnetApiCatalog).FullName);
        Assert.IsNotNull(type);
        var compilationLink = ReplaceSHA(SymbolUrlResolver.GetPdbSourceLinkUrl(compilation, type));
        Assert.IsTrue(compilationLink?.StartsWith("https://github.com/"));
        Assert.IsTrue(compilationLink?.EndsWith(".cs"));

        var method = type.GetMembers(nameof(DotnetApiCatalog.GenerateManagedReferenceYamlFiles)).FirstOrDefault();
        Assert.IsNotNull(method);
        var methodLink = ReplaceSHA(SymbolUrlResolver.GetPdbSourceLinkUrl(compilation, method));
        Assert.IsTrue(compilationLink?.StartsWith("https://github.com/"));
        Assert.IsTrue(compilationLink?.EndsWith(".cs"));

        static string ReplaceSHA(string value)
        {
            return Regex.Replace(value, "\\/[0-9a-zA-Z]{40}\\/", "/*/");
        }
    }
}
