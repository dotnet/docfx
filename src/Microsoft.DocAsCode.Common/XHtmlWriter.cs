/// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    public class XHtmlWriter : XmlTextWriter
    {
        private readonly HashSet<string> _voidElements;
        private string _currentElement;

        public XHtmlWriter(TextWriter writer)
            : base(writer)
        {
            // void element (ref: http://www.w3.org/TR/html-markup/syntax.html)
            _voidElements = new HashSet<string> { "area", "base", "br", "col", "command", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr" };
        }

        public override void WriteEndElement()
        {
            if (_voidElements.Contains(_currentElement))
            {
                base.WriteEndElement();
            }
            else
            {
                WriteFullEndElement();
            }
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            base.WriteStartElement(prefix, localName, ns);
            _currentElement = localName;
        }
    }
}
