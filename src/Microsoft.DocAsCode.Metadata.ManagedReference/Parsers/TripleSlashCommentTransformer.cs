// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.IO;
    using System.Net;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using System.Xml.Xsl;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public static class TripleSlashCommentTransformer
    {
        private static readonly XslCompiledTransform _transform;

        static TripleSlashCommentTransformer()
        {
            var assembly = typeof(TripleSlashCommentTransformer).Assembly;
            var xsltFilePath = $"{assembly.GetName().Name}.Transform.TripleSlashCommentTransform.xsl";
            using (var stream = assembly.GetManifestResourceStream(xsltFilePath))
            using (var reader = XmlReader.Create(stream))
            {
                var xsltSettings = new XsltSettings(true, true);
                _transform = new XslCompiledTransform();
                _transform.Load(reader, xsltSettings, new XmlUrlResolver());
            }
        }

        public static XDocument Transform(string xml, SyntaxLanguage language)
        {
            using (var ms = new MemoryStream())
            using (var writer = new XHtmlWriter(new StreamWriter(ms)))
            {
                XDocument doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
                var args = new XsltArgumentList();
                args.AddParam("language", "urn:input-variables", WebUtility.HtmlEncode(language.ToString().ToLower()));
                _transform.Transform(doc.CreateNavigator(), args, writer);
                ms.Seek(0, SeekOrigin.Begin);
                return XDocument.Load(ms, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
        }
    }
}
