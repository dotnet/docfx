// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using Microsoft.DocAsCode.Common;

    using Markdig.Syntax;

    public class MarkdownAstInterpreter : IInterpreter
    {
        private readonly IInterpreter _inner;

        public MarkdownAstInterpreter(IInterpreter inner)
        {
            _inner = inner;
        }

        public bool CanInterpret(BaseSchema schema)
        {
            return true;
        }

        public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
        {
            if (value == null || !CanInterpret(schema))
            {
                return value;
            }

            if (value is MarkdownDocument val)
            {
                return MarkupCore(val, context, path);
            }

            return _inner.Interpret(schema, value, context, path);
        }

        private static string MarkupCore(MarkdownDocument document, IProcessContext context, string path)
        {
            var host = context.Host;

            var mr = context.MarkdigMarkdownService.Render(document);
            (context.FileLinkSources).Merge(mr.FileLinkSources);
            (context.UidLinkSources).Merge(mr.UidLinkSources);
            (context.Dependency).UnionWith(mr.Dependency);
            return mr.Html;
        }
    }
}
