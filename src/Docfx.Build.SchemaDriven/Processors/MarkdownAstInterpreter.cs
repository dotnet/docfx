// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Markdig.Syntax;

namespace Docfx.Build.SchemaDriven.Processors;

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
            return MarkupCore(val, context);
        }

        return _inner.Interpret(schema, value, context, path);
    }

    private static string MarkupCore(MarkdownDocument document, IProcessContext context)
    {
        var host = context.Host;

        var mr = context.MarkdigMarkdownService.Render(document);
        context.FileLinkSources.Merge(mr.FileLinkSources);
        context.UidLinkSources.Merge(mr.UidLinkSources);
        context.Dependency.UnionWith(mr.Dependency);
        return mr.Html;
    }
}
