// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    public class DfmEngine : MarkdownEngine
    {
        public DfmEngine(IMarkdownContext context, IMarkdownRewriter rewriter, object renderer, Options options)
            : base(context, rewriter, renderer, options, new Dictionary<string, LinkObj>())
        {
        }

        public string Markup(string src, string path)
        {
            if (string.IsNullOrEmpty(src) && string.IsNullOrEmpty(path)) return string.Empty;
            // bug : Environment.CurrentDirectory = c:\a, path = d:\b, MakeRelativePath is not work ...
            path = PathUtility.MakeRelativePath(Environment.CurrentDirectory, path);
            return InternalMarkup(src, ImmutableStack<string>.Empty.Push(path));
        }

        public string InternalMarkup(string src, ImmutableStack<string> parents)
        {
            DfmEngine engine = new DfmEngine(Context, Rewriter, Renderer, Options);
            return Mark(Normalize(src), Context.SetFilePathStack(parents)).ToString();
        }

        public string InternalMarkup(string src, IMarkdownContext context)
        {
            DfmEngine engine = new DfmEngine(context, Rewriter, Renderer, Options);
            return Mark(Normalize(src), context).ToString();
        }
    }
}
