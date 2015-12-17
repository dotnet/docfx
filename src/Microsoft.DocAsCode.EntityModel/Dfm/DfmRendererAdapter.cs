// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmRendererAdapter : MarkdownRendererAdapter
    {
        public DfmRendererAdapter(DfmEngine engine, object renderer, Options options, Dictionary<string, LinkObj> links)
            : base(engine, renderer, options, links)
        {
            Engine = engine;
        }

        public new DfmEngine Engine { get; }

        public ImmutableStack<string> GetFilePathStack(IMarkdownContext context)
        {
            return (ImmutableStack<string>)context.Variables[DfmEngine.FilePathStackKey]; ;
        }

        public IMarkdownContext SetFilePathStack(IMarkdownContext context, ImmutableStack<string> filePathStack)
        {
            return context.CreateContext(context.Variables.SetItem(DfmEngine.FilePathStackKey, filePathStack));
        }
    }
}
