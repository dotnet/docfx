// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

using Docfx.Common;

namespace Docfx.Dotnet;

internal static class XmlCommentTransformer
{
    private static readonly XslCompiledTransform _transform;

    static XmlCommentTransformer()
    {
        var assembly = typeof(XmlCommentTransformer).Assembly;
        var xsltFilePath = $"{assembly.GetName().Name}.Resources.XmlCommentTransform.xsl";
        using var stream = assembly.GetManifestResourceStream(xsltFilePath);
        using var reader = XmlReader.Create(stream);
        var xsltSettings = new XsltSettings(true, true);
        _transform = new XslCompiledTransform();
        _transform.Load(reader, xsltSettings, new XmlUrlResolver());
    }

    public static XDocument Transform(string xml)
    {
        using var ms = new MemoryStream();
        using var writer = new XHtmlWriter(new StreamWriter(ms));
        XDocument doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        _transform.Transform(doc.CreateNavigator(), writer);
        ms.Seek(0, SeekOrigin.Begin);
        return XDocument.Load(ms, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
    }
}
