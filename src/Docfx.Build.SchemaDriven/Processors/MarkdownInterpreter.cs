// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.Build.SchemaDriven.Processors;

public class MarkdownInterpreter : IInterpreter
{
    public bool CanInterpret(BaseSchema schema)
    {
        return schema is { ContentType: ContentType.Markdown };
    }

    public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
    {
        if (value == null || !CanInterpret(schema))
        {
            return value;
        }

        if (value is not string val)
        {
            throw new ArgumentException($"{value.GetType()} is not supported type string.");
        }

        return MarkupCore(val, context, path);
    }

    private static string MarkupCore(string content, IProcessContext context, string path)
    {
        var host = context.Host;

        var mr = host.Markup(content, context.GetOriginalContentFile(path), false);
        context.FileLinkSources.Merge(mr.FileLinkSources);
        context.UidLinkSources.Merge(mr.UidLinkSources);
        context.Dependency.UnionWith(mr.Dependency);

        if (mr.Html.StartsWith("<p"))
            mr.Html = mr.Html.Insert(mr.Html.IndexOf(">"), " jsonPath=\"" + path + "\"");
        return mr.Html;
    }
}
