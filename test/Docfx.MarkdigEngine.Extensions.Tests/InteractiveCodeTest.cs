// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.MarkdigEngine.Tests;

[TestClass]
public class InteractiveCodeTest
{
    [TestMethod]
    [TestProperty("Related", "InteractiveCode")]
    public void TestInteractiveCode_CodeSnippetSimple()
    {
        TestUtility.VerifyMarkup(
            "[!code-azurecli-interactive[](InteractiveCode/sample.code)]",
            @"<pre><code class=""lang-azurecli"" data-interactive=""azurecli"">hello world!
</code></pre>",
            files: new Dictionary<string, string>
            {
                { "InteractiveCode/sample.code", "hello world!" }
            });
    }

    [TestMethod]
    [TestProperty("Related", "InteractiveCode")]
    public void TestInteractiveCode_FencedCodeSimple()
    {
        TestUtility.VerifyMarkup(
            @"```csharp-interactive
test
```",
            @"<pre><code class=""lang-csharp"" data-interactive=""csharp"">test
</code></pre>
");
    }

    [TestMethod]
    [TestProperty("Related", "InteractiveCode")]
    public void TestInteractiveCode_FencedCodeNonInteractiveSimple()
    {
        TestUtility.VerifyMarkup(
            @"```csharp
test
```",
            @"<pre><code class=""lang-csharp"">test
</code></pre>
");

        TestUtility.VerifyMarkup(
            @"```
test
```",
            @"<pre><code>test
</code></pre>
");
    }
}
