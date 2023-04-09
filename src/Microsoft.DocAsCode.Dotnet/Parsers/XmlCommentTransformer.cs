// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

using Microsoft.DocAsCode.Common;

namespace Microsoft.DocAsCode.Dotnet;

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
