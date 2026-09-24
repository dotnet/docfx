// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Docfx.Dotnet.Tests;

public class SourceLinkDeclarationTest
{
    [Theory]
    [InlineData(null, "/repo/Generated/Third.cs")]
    [InlineData("**/Generated/**", "/repo/Second.cs")]
    [InlineData("**/Second.cs", "/repo/Generated/Third.cs")]
    [InlineData("**/*.cs", "/repo/Generated/Third.cs")]
    public void UsesLastAllowedDeclarationAndRetainsSourceWhenAllAreExcluded(string pattern, string expectedPath)
    {
        var compilation = CompilationHelper.CreateCompilationFromCSharpCode("", new Dictionary<string, string>())
            .RemoveAllSyntaxTrees()
            .AddSyntaxTrees(
                CSharpSyntaxTree.ParseText("public partial class Widget { }", path: "/repo/First.cs", cancellationToken: TestContext.Current.CancellationToken),
                CSharpSyntaxTree.ParseText("\npublic partial class Widget { }", path: "/repo/Second.cs", cancellationToken: TestContext.Current.CancellationToken),
                CSharpSyntaxTree.ParseText("\n\npublic partial class Widget { }", path: "/repo/Generated/Third.cs", cancellationToken: TestContext.Current.CancellationToken));
        var type = compilation.GetTypeByMetadataName("Widget");
        var filter = new SourceLinkFilter(pattern is null ? [] : [pattern]);

        var source = VisitorHelper.GetSourceDetail(type, compilation, filter);

        Assert.Equal(expectedPath, source.Path);
        Assert.Equal(expectedPath == "/repo/Second.cs" ? 1 : 2, source.StartLine);
        if (pattern == "**/*.cs")
        {
            Assert.Null(source.Remote);
            Assert.Null(source.Href);
        }
    }
}
