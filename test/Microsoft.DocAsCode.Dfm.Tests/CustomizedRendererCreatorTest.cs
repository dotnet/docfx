// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Xunit;

    using Microsoft.DocAsCode.MarkdownLite;

    [Collection("docfx STA")]
    public class CustomizedRendererCreatorTest
    {
        [Fact]
        [Trait("Related", "dfm")]
        public void TestRendererCreator()
        {
            var content = @"namespace ConsoleApplication1
{
    // <namespace>
    using System;
    using System.Collections.Generic;
    using System.IO;
    // </namespace>

    // <snippetprogram>
    class Program
    {
        static void Main(string[] args)
        {
            string s = ""\ntest"";
            int i = 100;
        }
    }
    // </snippetprogram>

    #region Helper
    internal static class Helper
    {
        #region Foo
        public static void Foo()
        {
        }
        #endregion Foo
    }
    #endregion
}";
            File.WriteAllText("Program.csdocfx", content.Replace("\r\n", "\n"));
            var p = new TestDfmRendererPartProvider();
            var renderer = CustomizedRendererCreator.CreateRenderer(
                new DfmRenderer(),
                new[] { p },
                null);
            var engine = DocfxFlavoredMarked
                .CreateBuilder(".")
                .CreateDfmEngine(renderer);
            var result = engine.Markup(@"```cs
public void TestRendererCreator()
```
```cs-x
public void TestRendererCreator()
```
[!code-csdocfx[Main](Program.csdocfx#namespace ""This is root"")]
[!code-cs-xyz[](Program.csdocfx#Foo)]", "a.md");
            Assert.Equal(@"<pre><code class=""lang-cs"">public void TestRendererCreator()
</code></pre><pre class=""x""><code class=""lang-cs"">public void TestRendererCreator()
</code></pre><pre><code class=""lang-csdocfx"" name=""Main"" title=""This is root"">using System;
using System.Collections.Generic;
using System.IO;
</code></pre><pre><code class=""lang-cs-xyz"">public static void Foo()
{
}
</code></pre>".Replace("\r\n", "\n"), result);
            (renderer as IDisposable).Dispose();
            Assert.True(p.CodeRendererPartInstance.Disposed);
            if (File.Exists("Program.csdocfx"))
            {
                File.Delete("Program.csdocfx");
            }
        }

        private sealed class TestDfmRendererPartProvider : IDfmCustomizedRendererPartProvider
        {
            public CodeRendererPart CodeRendererPartInstance = new CodeRendererPart();

            public FencesCodeRendererPart FencesCodeRendererPartInstance = new FencesCodeRendererPart();

            public IEnumerable<IDfmCustomizedRendererPart> CreateParts(IReadOnlyDictionary<string, object> parameters)
            {
                yield return CodeRendererPartInstance;
                yield return FencesCodeRendererPartInstance;
            }

            public sealed class CodeRendererPart : DfmCustomizedRendererPartBase<IMarkdownRenderer, MarkdownCodeBlockToken, MarkdownBlockContext>, IDisposable
            {
                public bool Disposed { get; private set; }

                public override string Name => nameof(CodeRendererPart);

                public void Dispose()
                {
                    Disposed = true;
                }

                public override bool Match(IMarkdownRenderer renderer, MarkdownCodeBlockToken token, MarkdownBlockContext context)
                {
                    return token.Lang.EndsWith("-x");
                }

                public override StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeBlockToken token, MarkdownBlockContext context)
                {
                    StringBuffer result = "<pre class=\"x\"><code class=\"";
                    result += renderer.Options.LangPrefix;
                    result += StringHelper.Escape(token.Lang.Remove(token.Lang.Length - 2), true);
                    result += "\">";
                    result += token.Code;
                    result += "\n</code></pre>";
                    return result;
                }
            }

            public sealed class FencesCodeRendererPart : DfmCustomizedRendererPartBase<IMarkdownRenderer, DfmFencesToken, MarkdownBlockContext>, IDisposable
            {
                private DfmCodeRenderer _codeRenderer;

                public bool Disposed { get; private set; }

                public override string Name => nameof(FencesCodeRendererPart);

                public FencesCodeRendererPart()
                {
                    _codeRenderer = new DfmCodeRenderer(TagNameBlockPathQueryOption
                        .GetDefaultCodeLanguageExtractorsBuilder()
                        .AddAlias("csharp", "csdocfx", ".csdocfx")
                        .AddAlias(x => x.StartsWith(".") ? null : x + "-xyz"));
                }

                public void Dispose()
                {
                    Disposed = true;
                }

                public override bool Match(IMarkdownRenderer renderer, DfmFencesToken token, MarkdownBlockContext context)
                {
                    return true;
                }

                public override StringBuffer Render(IMarkdownRenderer renderer, DfmFencesToken token, MarkdownBlockContext context)
                {
                    return _codeRenderer.Render(renderer, token, context);
                }
            }
        }
    }
}