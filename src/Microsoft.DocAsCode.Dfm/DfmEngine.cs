// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    public class DfmEngine : MarkdownEngine
    {
        public DfmEngine(IMarkdownContext context, IMarkdownTokenRewriter rewriter, object renderer, Options options)
            : base(context, rewriter, renderer, options, new Dictionary<string, LinkObj>())
        {
        }

        public string Markup(string src, string path)
        {
            if (string.IsNullOrEmpty(src))
            {
                return string.Empty;
            }
            return InternalMarkup(src, ImmutableStack.Create(path));
        }

        internal string InternalMarkup(string src, ImmutableStack<string> parents)
        {
            LoggerFileScope fileScope = null;
            if (!parents.IsEmpty)
            {
                var path = parents.Peek().ToDisplayPath();
                if (!string.IsNullOrEmpty(path))
                {
                    fileScope = new LoggerFileScope(path);
                }
            }

            using (fileScope)
            {
                return InternalMarkup(src, Context.SetFilePathStack(parents));
            }
        }

        internal string InternalMarkup(string src, IMarkdownContext context) =>
            Mark(Normalize(src), context).ToString();
    }
}
