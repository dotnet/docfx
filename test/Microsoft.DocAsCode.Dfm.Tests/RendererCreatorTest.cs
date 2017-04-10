// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Composition.Hosting;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Xml;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.MarkdownLite;

    public partial class RendererCreatorTest
    {
        [Fact]
        [Trait("Related", "dfm")]
        public void TestRendererCreator()
        {
            var engine = DocfxFlavoredMarked
                .CreateBuilder(".")
                .CreateDfmEngine(
                    RendererCreator.CreateRenderer(
                        new DfmRenderer(),
                        new[] { new TestDfmRendererPartProvider() },
                        null));
            var result = engine.Markup(@"```cs
public void TestRendererCreator()
```
```cs-x
public void TestRendererCreator()
```", "a.md");
            Assert.Equal(@"<pre><code class=""lang-cs"">public void TestRendererCreator()
</code></pre><pre class=""x""><code class=""lang-cs"">public void TestRendererCreator()
</code></pre>".Replace("\r\n", "\n"), result);
        }

        private sealed class TestDfmRendererPartProvider : IDfmRendererPartProvider
        {
            public IEnumerable<IDfmRendererPart> CreateParts(IReadOnlyDictionary<string, object> paramters)
            {
                yield return new CodeRendererPart();
            }

            private sealed class CodeRendererPart : IDfmRendererPart
            {
                public string Name => nameof(CodeRendererPart);

                public Type MarkdownRendererType => typeof(IMarkdownRenderer);

                public Type MarkdownTokenType => typeof(MarkdownCodeBlockToken);

                public Type MarkdownContextType => typeof(MarkdownBlockContext);

                public bool Match(IMarkdownRenderer renderer, IMarkdownToken token, IMarkdownContext context)
                {
                    var code = (MarkdownCodeBlockToken)token;
                    return code.Lang.EndsWith("-x");
                }

                public StringBuffer Render(IMarkdownRenderer renderer, IMarkdownToken token, IMarkdownContext context)
                {
                    var code = (MarkdownCodeBlockToken)token;
                    StringBuffer result = "<pre class=\"x\"><code class=\"";
                    result += renderer.Options.LangPrefix;
                    result += StringHelper.Escape(code.Lang.Remove(code.Lang.Length - 2), true);
                    result += "\">";
                    result += code.Code;
                    result += "\n</code></pre>";
                    return result;
                }
            }
        }
    }
}
