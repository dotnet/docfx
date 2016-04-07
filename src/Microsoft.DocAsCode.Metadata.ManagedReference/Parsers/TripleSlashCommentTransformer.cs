namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using System.Xml.Xsl;

    public class TripleSlashCommentTransformer
    {
        private readonly XslCompiledTransform _transform;

        public TripleSlashCommentTransformer(Assembly assembly, string dir, string xsltPath)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            var resourceName = $"{assembly.GetName().Name}.{dir}.{xsltPath}";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = XmlReader.Create(stream))
            {
                var xsltSettings = new XsltSettings(true, true);
                _transform = new XslCompiledTransform();
                _transform.Load(reader, xsltSettings, new XmlUrlResolver());
            }

        }

        public XDocument Transform(XDocument doc)
        {
            using (var ms = new MemoryStream())
            using (var writer = XmlWriter.Create(ms))
            {
                _transform.Transform(doc.CreateNavigator(), writer);
                ms.Seek(0, SeekOrigin.Begin);
                return XDocument.Load(ms, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
        }
    }
}
