// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
    using System;
    using System.Collections.Generic;

    using Xunit;

    using Microsoft.DocAsCode.MarkdownLite;

    public class CustomizedRendererCreatorTest
    {
        [Fact]
        [Trait("Related", "dfm")]
        public void TestRendererCreator()
        {
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
```", "a.md");
            Assert.Equal(@"<pre><code class=""lang-cs"">public void TestRendererCreator()
</code></pre><pre class=""x""><code class=""lang-cs"">public void TestRendererCreator()
</code></pre>".Replace("\r\n", "\n"), result);
            (renderer as IDisposable).Dispose();
            Assert.True(p.CodeRendererPartInstance.Disposed);
        }

        private sealed class TestDfmRendererPartProvider : IDfmCustomizedRendererPartProvider
        {
            public CodeRendererPart CodeRendererPartInstance = new CodeRendererPart();

            public IEnumerable<IDfmCustomizedRendererPart> CreateParts(IReadOnlyDictionary<string, object> parameters)
            {
                yield return CodeRendererPartInstance;
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
        }
    }
}
