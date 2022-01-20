using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using Mono.Cecil;

namespace Mono.Documentation.Updater
{
   public class MsxdocDocumentationImporter : DocumentationImporter
    {
        XmlDocument slashdocs;
        Dictionary<string, XmlNode> slashdocsMapping = new Dictionary<string, XmlNode>();

        public MsxdocDocumentationImporter (string file)
        {
            try
            {
                char oppositeSlash = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
                if (file.Contains (oppositeSlash))
                    file = file.Replace (oppositeSlash, Path.DirectorySeparatorChar);

                var xml = File.ReadAllText (file);

                // Ensure Unix line endings
                xml = xml.Replace ("\r", "");

                slashdocs = new XmlDocument ();

                slashdocs.LoadXml (xml);

                foreach(XmlNode node in slashdocs.SelectNodes("doc/members/member"))
                {
                    var sig = node.Attributes["name"]?.Value;
                    if(!string.IsNullOrEmpty(sig))
                    {
                        slashdocsMapping[sig] = node;
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine ($"Importer Error: {ex.Message}");
            }
        }

        public override void ImportDocumentation (DocsNodeInfo info)
        {
            //first try C# compiler docIds, next other languages
            XmlNode elem = GetDocs(info.Member ?? info.Type, MDocUpdater.csharpSlashdocFormatter) ??
                           GetDocs(info.Member ?? info.Type, MDocUpdater.msxdocxSlashdocFormatter);

            if (elem == null)
                return;

            XmlElement e = info.Node;

            if (e.SelectNodes("./*[@overwrite]").Count == 0)
            {
                // there are no overwrites in this node, just clear everything except for default nodes and nodes that don't have an incoming equivalent
                DocUtils.ClearNodesIfNotDefault(e, elem);
            }

            if (elem.SelectSingleNode("summary") != null && DocUtils.NeedsOverwrite(e["summary"]))
                MDocUpdater.ClearElement(e, "summary");
            if (elem.SelectSingleNode("remarks") != null && DocUtils.NeedsOverwrite(e["remarks"]))
                MDocUpdater.ClearElement(e, "remarks");
            if (elem.SelectSingleNode("value") != null || elem.SelectSingleNode("returns") != null)
            {
                if (DocUtils.NeedsOverwrite(e["value"]))
                    MDocUpdater.ClearElement(e, "value");
                if (DocUtils.NeedsOverwrite(e["returns"]))
                    MDocUpdater.ClearElement(e, "returns");
            }
            
            

            foreach (XmlNode child in elem.ChildNodes)
            {
                switch (child.Name)
                {
                    case "param":
                    case "typeparam":
                        {
                            XmlAttribute name = child.Attributes["name"];
                            if (name == null)
                                break;
                            XmlElement p2 = (XmlElement)e.SelectSingleNode (child.Name + "[@name='" + name.Value + "']");
                            if (p2 == null)
                            {
                                p2 = e.OwnerDocument.CreateElement(child.Name);
                                var pname = e.OwnerDocument.CreateAttribute("name");
                                pname.Value = name.Value;

                                p2.Attributes.Append(pname);
                                e.AppendChild(p2);
                            }
                            if (DocUtils.NeedsOverwrite(p2))
                            {
                                p2.RemoveAttribute("overwrite");
                                p2.InnerXml = child.InnerXml;
                                
                            }
                            break;
                        }
                    // Occasionally XML documentation will use <returns/> on
                    // properties, so let's try to normalize things.
                    case "value":
                    case "returns":
                        {
                            XmlElement v = e.OwnerDocument.CreateElement (info.ReturnIsReturn ? "returns" : "value");
                            XmlElement p2 = (XmlElement)e.SelectSingleNode(child.Name);
                            if (p2 == null)
                            {
                                v.InnerXml = child.InnerXml;
                                e.AppendChild(v);
                            }
                            break;
                        }
                    case "altmember":
                    case "exception":
                    case "permission":
                        {
                            XmlAttribute cref = child.Attributes["cref"] ?? child.Attributes["name"];
                            if (cref == null)
                                break;
                            XmlElement a = (XmlElement)e.SelectSingleNode (child.Name + "[@cref='" + cref.Value + "']");
                            if (a == null)
                            {
                                a = e.OwnerDocument.CreateElement (child.Name);
                                a.SetAttribute ("cref", cref.Value);
                                e.AppendChild (a);
                            }
                            if(DocUtils.NeedsOverwrite(a))
                                a.InnerXml = child.InnerXml;
                            break;
                        }
                    case "seealso":
                        {
                            XmlAttribute cref = child.Attributes["cref"];
                            if (cref == null)
                                break;
                            XmlElement a = (XmlElement)e.SelectSingleNode ("altmember[@cref='" + cref.Value + "']");
                            if (a == null)
                            {
                                a = e.OwnerDocument.CreateElement ("altmember");
                                a.SetAttribute ("cref", cref.Value);
                                e.AppendChild (a);
                            }
                            break;
                        }
                    default:
                        {
                            var targetNodes = e.ChildNodes.Cast<XmlNode> ()
                                .Where (n => n.Name == child.Name)
                                .Select (n => new
                                {
                                    Xml = n.OuterXml,
                                    Overwrite = n.Attributes != null ? n.Attributes["overwrite"] : null
                                });
                            string sourceXml = child.OuterXml;

                            if (!targetNodes.Any (n => sourceXml.Equals (n.Xml) || n.Overwrite?.Value == "false"))
                            {
                                MDocUpdater.CopyNode(child, e);
                            }
                            break;
                        }
                }
            }
        }

        private XmlNode GetDocs (MemberReference member, MemberFormatter formatter)
        {
            string slashdocsig = formatter?.GetDeclaration (member);
            XmlNode mappedValue;
            if (slashdocsig != null && slashdocs != null && slashdocsMapping.TryGetValue(slashdocsig, out mappedValue))
                return mappedValue;
            return null;
        }

        public override bool CheckRemoveByMapping(DocsNodeInfo info, string xmlChildName)
        {
            XmlNode elem = GetDocs(info.Member ?? info.Type, MDocUpdater.csharpSlashdocFormatter) ??
                                     GetDocs(info.Member ?? info.Type, MDocUpdater.msxdocxSlashdocFormatter);

            if (elem != null)
            {
                foreach (XmlNode child in elem.ChildNodes)
                {
                    if (child.Name == xmlChildName)
                    { return true; }
                }
            }

            return false;
        }
    }
}
