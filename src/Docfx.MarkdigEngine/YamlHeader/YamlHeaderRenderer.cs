// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Docfx.Common;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

public class YamlHeaderRenderer : HtmlObjectRenderer<YamlFrontMatterBlock>
{
    private readonly MarkdownContext _context;

    public YamlHeaderRenderer(MarkdownContext context)
    {
        _context = context;
    }

    protected override void Write(HtmlRenderer renderer, YamlFrontMatterBlock obj)
    {
        if (InclusionContext.IsInclude)
        {
            return;
        }

        var content = obj.Lines.ToString();
        try
        {
            using StringReader reader = new(content);
            var result = YamlUtility.Deserialize<Dictionary<string, object>>(reader);
            if (result != null)
            {
                renderer.Write("<yamlheader").Write($" start=\"{obj.Line + 1}\" end=\"{obj.Line + obj.Lines.Count + 2}\"");
                renderer.WriteAttributes(obj).Write(">");
                renderer.Write(WebUtility.HtmlEncode(obj.Lines.ToString()));
                renderer.Write("</yamlheader>");
            }
        }
        catch (Exception ex)
        {
            // not a valid yml header, do nothing
            _context.LogWarning("invalid-yaml-header", ex.Message, obj);
        }
    }
}
