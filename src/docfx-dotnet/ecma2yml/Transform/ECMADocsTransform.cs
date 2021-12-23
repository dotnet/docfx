namespace ECMA2Yaml
{
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using System.Xml.Xsl;

    /// <summary>
    /// Thread safe, can be used in multi threads after loaded
    /// </summary>
    public class ECMADocsTransform
    {
        private readonly XslCompiledTransform _transform;

        public ECMADocsTransform()
        {
            var assembly = this.GetType().Assembly;
            var xsltFilePath = $"ECMA2Yaml.Transform.ECMADocsTransform.xsl";
            using (var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "data/ECMADocsTransform.xsl")))
            using (var reader = XmlReader.Create(stream))
            {
                var xsltSettings = new XsltSettings(true, true);
                _transform = new XslCompiledTransform();
                _transform.Load(reader, xsltSettings, new XmlUrlResolver());
            }
        }

        public XDocument Transform(string xml)
        {
            using (var ms = new MemoryStream())
            using (var writer = new XHtmlWriter(new StreamWriter(ms)))
            {
                XDocument doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
                var args = new XsltArgumentList();
                _transform.Transform(doc.CreateNavigator(), args, writer);
                ms.Seek(0, SeekOrigin.Begin);
                return XDocument.Load(ms, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
        }
    }
}
