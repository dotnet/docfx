using System;
using System.Xml;

using StringList = System.Collections.Generic.List<string>;

namespace Mono.Documentation.Updater
{
    class EcmaDocumentationImporter : DocumentationImporter
    {

        XmlReader ecmadocs;

        public EcmaDocumentationImporter (XmlReader ecmaDocs)
        {
            this.ecmadocs = ecmaDocs;
        }

        public override void ImportDocumentation (DocsNodeInfo info)
        {
            if (!ecmadocs.IsStartElement ("Docs"))
            {
                return;
            }

            XmlElement e = info.Node;

            int depth = ecmadocs.Depth;
            ecmadocs.ReadStartElement ("Docs");
            while (ecmadocs.Read ())
            {
                if (ecmadocs.Name == "Docs")
                {
                    if (ecmadocs.Depth == depth && ecmadocs.NodeType == XmlNodeType.EndElement)
                        break;
                    else
                        throw new InvalidOperationException ("Skipped past current <Docs/> element!");
                }
                if (!ecmadocs.IsStartElement ())
                    continue;
                switch (ecmadocs.Name)
                {
                    case "param":
                    case "typeparam":
                        {
                            string name = ecmadocs.GetAttribute ("name");
                            if (name == null)
                                break;
                            XmlNode doc = e.SelectSingleNode (
                                    ecmadocs.Name + "[@name='" + name + "']");
                            string value = ecmadocs.ReadInnerXml ();
                            if (doc != null)
                                doc.InnerXml = value.Replace ("\r", "");
                            break;
                        }
                    case "altmember":
                    case "exception":
                    case "permission":
                    case "seealso":
                        {
                            string name = ecmadocs.Name;
                            string cref = ecmadocs.GetAttribute ("cref");
                            if (cref == null)
                                break;
                            XmlNode doc = e.SelectSingleNode (
                                    ecmadocs.Name + "[@cref='" + cref + "']");
                            string value = ecmadocs.ReadInnerXml ().Replace ("\r", "");
                            if (doc != null)
                                doc.InnerXml = value;
                            else
                            {
                                XmlElement n = e.OwnerDocument.CreateElement (name);
                                n.SetAttribute ("cref", cref);
                                n.InnerXml = value;
                                e.AppendChild (n);
                            }
                            break;
                        }
                    default:
                        {
                            string name = ecmadocs.Name;
                            string xpath = ecmadocs.Name;
                            StringList attributes = new StringList (ecmadocs.AttributeCount);
                            if (ecmadocs.MoveToFirstAttribute ())
                            {
                                do
                                {
                                    attributes.Add ("@" + ecmadocs.Name + "=\"" + ecmadocs.Value + "\"");
                                } while (ecmadocs.MoveToNextAttribute ());
                                ecmadocs.MoveToContent ();
                            }
                            if (attributes.Count > 0)
                            {
                                xpath += "[" + string.Join (" and ", attributes.ToArray ()) + "]";
                            }
                            XmlNode doc = e.SelectSingleNode (xpath);
                            string value = ecmadocs.ReadInnerXml ().Replace ("\r", "");
                            if (doc != null)
                            {
                                doc.InnerXml = value;
                            }
                            else
                            {
                                XmlElement n = e.OwnerDocument.CreateElement (name);
                                n.InnerXml = value;
                                foreach (string a in attributes)
                                {
                                    int eq = a.IndexOf ('=');
                                    n.SetAttribute (a.Substring (1, eq - 1), a.Substring (eq + 2, a.Length - eq - 3));
                                }
                                e.AppendChild (n);
                            }
                            break;
                        }
                }
            }
        }

        public override bool CheckRemoveByMapping(DocsNodeInfo info, string xmlChildName)
        {
            if (!ecmadocs.IsStartElement("Docs"))
            {
                return false;
            }

            ecmadocs.ReadStartElement("Docs");
            while (ecmadocs.Read())
            {
                if (!ecmadocs.IsStartElement())
                    continue;
                if (ecmadocs.Name == xmlChildName)
                    return true;
            }

            return false;
        }
    }
}