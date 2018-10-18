// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using Markdig;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdigEngine.Extensions;
    using Microsoft.DocAsCode.Plugins;

    using Xunit;

    [Collection("docfx STA")]
    public class FallbackTest
    {
        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestFallback_Inclusion_random_name()
        {
            // -root_folder (this is also docset folder)
            //  |- root.md
            //  |- a_folder
            //  |  |- a.md
            //  |- token_folder
            //  |  |- token1.md
            // -fallback_folder
            //  |- token_folder
            //     |- token2.md

            // 1. Prepare data
            var uniqueFolderName = Path.GetRandomFileName();
            var root = $@"1markdown root.md main content start.

[!include[a](a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md ""This is a.md"")]

markdown root.md main content end.";

            var a = $@"1markdown a.md main content start.

[!include[token1](../token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md ""This is token1.md"")]
[!include[token1](../token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md ""This is token2.md"")]

markdown a.md main content end.";

            var token1 = $@"1markdown token1.md content start.

[!include[token2](token2_{uniqueFolderName}.md ""This is token2.md"")]

markdown token1.md content end.";

            var token2 = @"**1markdown token2.md main content**";

            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/root_{uniqueFolderName}.md", root);
            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md", a);
            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md", token1);
            TestUtility.WriteToFile($"{uniqueFolderName}/fallback_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md", token2);

            try
            {
                var fallbackFolders = new List<string> { { Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/fallback_folder_{uniqueFolderName}") } };
                var basePath = $"{uniqueFolderName}/root_folder_{uniqueFolderName}";
                SetEnvironmentContext(basePath, fallbackFolders);

                var parameter = new MarkdownServiceParameters
                {
                    BasePath = basePath,
                    Extensions = new Dictionary<string, object>
                {
                    { "EnableSourceInfo", false }
                }
                };
                var service = new MarkdigMarkdownService(parameter);
                var marked = service.Markup(root, $"root_{uniqueFolderName}.md");
                var dependency = marked.Dependency;

                Assert.Equal($@"<p>1markdown root.md main content start.</p>
<p>1markdown a.md main content start.</p>
<p>1markdown token1.md content start.</p>
<p><strong>1markdown token2.md main content</strong></p>

<p>markdown token1.md content end.</p>
<p><strong>1markdown token2.md main content</strong></p>

<p>markdown a.md main content end.</p>

<p>markdown root.md main content end.</p>
".Replace("\r\n", "\n"), marked.Html);
                Assert.Equal(
                    new[] { $"../fallback_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md", $"a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md", $"token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md", $"token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md" },
                    dependency.OrderBy(x => x).ToArray());
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestFallback_InclusionWithCodeFences()
        {
            // -root_folder (this is also docset folder)
            //  |- root.md
            //  |- a_folder
            //     |- a.md
            //  |- code_folder
            //     |- sample1.cs
            // -fallback_folder
            //  |- a_folder
            //     |- code_in_a.cs
            //  |- code_folder
            //     |- sample2.cs

            // 1. Prepare data
            var root = @"markdown root.md main content start.

mardown a content in root.md content start

[!include[a](a_folder/a.md ""This is a.md"")]

mardown a content in root.md content end

sample 1 code in root.md content start

[!CODE-cs[this is sample 1 code](code_folder/sample1.cs)]

sample 1 code in root.md content end

sample 2 code in root.md content start

[!CODE-cs[this is sample 2 code](code_folder/sample2.cs)]

sample 2 code in root.md content end

markdown root.md main content end.";

            var a = @"markdown a.md main content start.

code_in_a code in a.md content start

[!CODE-cs[this is code_in_a code](code_in_a.cs)]

code_in_a in a.md content end

markdown a.md a.md content end.";

            var code_in_a = @"namespace code_in_a{}";

            var sample1 = @"namespace sample1{}";

            var sample2 = @"namespace sample2{}";

            var uniqueFolderName = Path.GetRandomFileName();
            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder/root.md", root);
            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder/a_folder/a.md", a);
            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder/code_folder/sample1.cs", sample1);
            TestUtility.WriteToFile($"{uniqueFolderName}/fallback_folder/a_folder/code_in_a.cs", code_in_a);
            TestUtility.WriteToFile($"{uniqueFolderName}/fallback_folder/code_folder/sample2.cs", sample2);

            try
            {
                var fallbackFolders = new List<string> { { Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/fallback_folder") } };
                var basePath = $"{uniqueFolderName}/root_folder";
                SetEnvironmentContext(basePath, fallbackFolders);
                // Verify root.md markup result
                var parameter = new MarkdownServiceParameters
                {
                    BasePath = basePath,
                    Extensions = new Dictionary<string, object>
                {
                    { "EnableSourceInfo", false }
                }
                };
                var service = new MarkdigMarkdownService(parameter);
                var rootMarked = service.Markup(root, "root.md");
                var rootDependency = rootMarked.Dependency;
                Assert.Equal(@"<p>markdown root.md main content start.</p>
<p>mardown a content in root.md content start</p>
<p>markdown a.md main content start.</p>
<p>code_in_a code in a.md content start</p>
<pre><code class=""lang-cs"" name=""this is code_in_a code"">namespace code_in_a{}
</code></pre>
<p>code_in_a in a.md content end</p>
<p>markdown a.md a.md content end.</p>

<p>mardown a content in root.md content end</p>
<p>sample 1 code in root.md content start</p>
<pre><code class=""lang-cs"" name=""this is sample 1 code"">namespace sample1{}
</code></pre>
<p>sample 1 code in root.md content end</p>
<p>sample 2 code in root.md content start</p>
<pre><code class=""lang-cs"" name=""this is sample 2 code"">namespace sample2{}
</code></pre>
<p>sample 2 code in root.md content end</p>
<p>markdown root.md main content end.</p>
".Replace("\r\n", "\n"), rootMarked.Html);
                Assert.Equal(
                    new[] { "../fallback_folder/a_folder/code_in_a.cs", "../fallback_folder/code_folder/sample2.cs", "a_folder/a.md", "a_folder/code_in_a.cs", "code_folder/sample1.cs", "code_folder/sample2.cs" },
                    rootDependency.OrderBy(x => x).ToArray());

                // Verify a.md markup result
                //var aMarked = service.Markup(Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/root_folder"), a, fallbackFolders, "a_folder/a.md");
                var aMarked = service.Markup(a, "a_folder/a.md");
                var aDependency = aMarked.Dependency;
                Assert.Equal(@"<p>markdown a.md main content start.</p>
<p>code_in_a code in a.md content start</p>
<pre><code class=""lang-cs"" name=""this is code_in_a code"">namespace code_in_a{}
</code></pre>
<p>code_in_a in a.md content end</p>
<p>markdown a.md a.md content end.</p>
".Replace("\r\n", "\n"), aMarked.Html);
                Assert.Equal(
                    new[] { "../../fallback_folder/a_folder/code_in_a.cs", "code_in_a.cs" },
                    aDependency.OrderBy(x => x).ToArray());
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        private void SetEnvironmentContext(string baseDirectory, List<string> fallbackFolders)
        {
            EnvironmentContext.SetBaseDirectory(baseDirectory);
            FileAbstractLayerBuilder falBuilder = FileAbstractLayerBuilder.Default
                            .ReadFromRealFileSystem(EnvironmentContext.BaseDirectory);

            foreach (var fallbackFolder in fallbackFolders)
            {
                var fallbackReader = new RealFileReader(fallbackFolder, ImmutableDictionary<string, string>.Empty);
                falBuilder = falBuilder.FallbackReadFromReader(fallbackReader);
            }

            EnvironmentContext.FileAbstractLayerImpl = falBuilder.Create();
        }
    }
}
