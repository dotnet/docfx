// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.Plugins;

    [Collection("docfx STA")]
    public class DocfxFlavoredMarkdownFallbackTest
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

            WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/root_{uniqueFolderName}.md", root);
            WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md", a);
            WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md", token1);
            WriteToFile($"{uniqueFolderName}/fallback_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md", token2);

            try
            {
                EnvironmentContext.SetBaseDirectory($"{uniqueFolderName}/root_folder_{uniqueFolderName}");
                var fallbackFolders = new List<string> { { Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/fallback_folder_{uniqueFolderName}") } };
                var dependency = new HashSet<string>();
                var marked = DocfxFlavoredMarked.Markup(Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/root_folder_{uniqueFolderName}"), root, fallbackFolders, $"root_{uniqueFolderName}.md", dependency: dependency);
                Assert.Equal($@"<p>1markdown root.md main content start.</p>
<p>1markdown a.md main content start.</p>
<p>1markdown token1.md content start.</p>
<p><strong>1markdown token2.md main content</strong></p>
<p>markdown token1.md content end.</p>
<p><strong>1markdown token2.md main content</strong></p>
<p>markdown a.md main content end.</p>
<p>markdown root.md main content end.</p>
".Replace("\r\n", "\n"), marked);
                Assert.Equal(
                    new[] { $"../fallback_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md", $"a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md", $"token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md", $"token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md" },
                    dependency.OrderBy(x => x));
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
            WriteToFile($"{uniqueFolderName}/root_folder/root.md", root);
            WriteToFile($"{uniqueFolderName}/root_folder/a_folder/a.md", a);
            WriteToFile($"{uniqueFolderName}/root_folder/code_folder/sample1.cs", sample1);
            WriteToFile($"{uniqueFolderName}/fallback_folder/a_folder/code_in_a.cs", code_in_a);
            WriteToFile($"{uniqueFolderName}/fallback_folder/code_folder/sample2.cs", sample2);

            try
            {
                EnvironmentContext.SetBaseDirectory($"{uniqueFolderName}/root_folder");
                var fallbackFolders = new List<string> { { Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/fallback_folder") } };

                // Verify root.md markup result
                var rootDependency = new HashSet<string>();
                var rootMarked = DocfxFlavoredMarked.Markup(Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/root_folder"), root, fallbackFolders, "root.md", dependency: rootDependency);
                Assert.Equal(@"<p>markdown root.md main content start.</p>
<p>mardown a content in root.md content start</p>
<p>markdown a.md main content start.</p>
<p>code_in_a code in a.md content start</p>
<pre><code class=""lang-cs"" name=""this is code_in_a code"">namespace code_in_a{}
</code></pre><p>code_in_a in a.md content end</p>
<p>markdown a.md a.md content end.</p>
<p>mardown a content in root.md content end</p>
<p>sample 1 code in root.md content start</p>
<pre><code class=""lang-cs"" name=""this is sample 1 code"">namespace sample1{}
</code></pre><p>sample 1 code in root.md content end</p>
<p>sample 2 code in root.md content start</p>
<pre><code class=""lang-cs"" name=""this is sample 2 code"">namespace sample2{}
</code></pre><p>sample 2 code in root.md content end</p>
<p>markdown root.md main content end.</p>
".Replace("\r\n", "\n"), rootMarked);
                Assert.Equal(
                    new[] { "../fallback_folder/a_folder/code_in_a.cs", "../fallback_folder/code_folder/sample2.cs", "a_folder/a.md", "a_folder/code_in_a.cs", "code_folder/sample1.cs", "code_folder/sample2.cs" },
                    rootDependency.OrderBy(x => x));

                // Verify a.md markup result
                var aDependency = new HashSet<string>();
                var aMarked = DocfxFlavoredMarked.Markup(Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/root_folder"), a, fallbackFolders, "a_folder/a.md", dependency: aDependency);
                Assert.Equal(@"<p>markdown a.md main content start.</p>
<p>code_in_a code in a.md content start</p>
<pre><code class=""lang-cs"" name=""this is code_in_a code"">namespace code_in_a{}
</code></pre><p>code_in_a in a.md content end</p>
<p>markdown a.md a.md content end.</p>
".Replace("\r\n", "\n"), aMarked);
                Assert.Equal(
                    new[] { "../../fallback_folder/a_folder/code_in_a.cs", "code_in_a.cs" },
                    aDependency.OrderBy(x => x));
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        private static void WriteToFile(string file, string content)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(file, content);
        }
    }
}
