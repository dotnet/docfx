using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Mono.Cecil;
using Mono.Documentation.Updater.Frameworks;
using Mono.Documentation.Util;

namespace Mono.Documentation.Updater
{
    class EcmaDocumentationEnumerator : DocumentationEnumerator
    {

        XmlReader ecmadocs;
        MDocUpdater app;

        public EcmaDocumentationEnumerator (MDocUpdater app, XmlReader ecmaDocs)
        {
            this.app = app;
            this.ecmadocs = ecmaDocs;
        }

        public override IEnumerable<TypeDefinition> GetDocumentationTypes (AssemblyDefinition assembly, List<string> forTypes)
        {
            HashSet<string> seen = new HashSet<string> ();
            return GetDocumentationTypes (assembly, forTypes, seen)
                .Concat (base.GetDocumentationTypes (assembly, forTypes, seen));
        }

        new IEnumerable<TypeDefinition> GetDocumentationTypes (AssemblyDefinition assembly, List<string> forTypes, HashSet<string> seen)
        {
            int typeDepth = -1;
            while (ecmadocs.Read ())
            {
                switch (ecmadocs.Name)
                {
                    case "Type":
                        {
                            if (typeDepth == -1)
                                typeDepth = ecmadocs.Depth;
                            if (ecmadocs.NodeType != XmlNodeType.Element)
                                continue;
                            if (typeDepth != ecmadocs.Depth) // nested <TypeDefinition/> element?
                                continue;
                            string typename = ecmadocs.GetAttribute ("FullName");
                            string typename2 = MDocUpdater.GetTypeFileName (typename);
                            if (forTypes != null &&
                                    forTypes.BinarySearch (typename) < 0 &&
                                    typename != typename2 &&
                                    forTypes.BinarySearch (typename2) < 0)
                                continue;
                            TypeDefinition t;
                            if ((t = assembly.GetType (typename)) == null &&
                                    (t = assembly.GetType (typename2)) == null)
                                continue;
                            seen.Add (typename);
                            if (typename != typename2)
                                seen.Add (typename2);
                            Console.WriteLine ("  Import: {0}", t.FullName);
                            if (ecmadocs.Name != "Docs")
                            {
                                int depth = ecmadocs.Depth;
                                while (ecmadocs.Read ())
                                {
                                    if (ecmadocs.Name == "Docs" && ecmadocs.Depth == depth + 1)
                                        break;
                                }
                            }
                            if (!ecmadocs.IsStartElement ("Docs"))
                                throw new InvalidOperationException ("Found " + ecmadocs.Name + "; expecting <Docs/>!");
                            yield return t;
                            break;
                        }
                    default:
                        break;
                }
            }
        }

        public override IEnumerable<DocsNodeInfo> GetDocumentationMembers (XmlDocument basefile, TypeDefinition type, FrameworkTypeEntry typeEntry)
        {
            return GetMembers (basefile, type, typeEntry)
                .Concat (base.GetDocumentationMembers (basefile, type, typeEntry));
        }

        private IEnumerable<DocsNodeInfo> GetMembers (XmlDocument basefile, TypeDefinition type, FrameworkTypeEntry typeEntry)
        {
            while (ecmadocs.Name != "Members" && ecmadocs.Read ())
            {
                // do nothing
            }
            if (ecmadocs.IsEmptyElement)
                yield break;

            int membersDepth = ecmadocs.Depth;
            bool go = true;
            while (go && ecmadocs.Read ())
            {
                switch (ecmadocs.Name)
                {
                    case "Member":
                        {
                            if (membersDepth != ecmadocs.Depth - 1 || ecmadocs.NodeType != XmlNodeType.Element)
                                continue;
                            DocumentationMember dm = new DocumentationMember (ecmadocs);

                            string xp = MDocUpdater.GetXPathForMember (dm);
                            XmlElement oldmember = (XmlElement)basefile.SelectSingleNode (xp);
                            MemberReference m;
                            if (oldmember == null)
                            {
                                m = GetMember (type, dm);
                                if (m == null)
                                {
                                    app.Warning ("Could not import ECMA docs for `{0}'s `{1}': Member not found.",
                                            type.FullName, dm.MemberSignatures["C#"]);
                                    // SelectSingleNode (ecmaDocsMember, "MemberSignature[@Language=\"C#\"]/@Value").Value);
                                    continue;
                                }
                                // oldmember lookup may have failed due to type parameter renames.
                                // Try again.
                                oldmember = (XmlElement)basefile.SelectSingleNode (MDocUpdater.GetXPathForMember (m)); //todo: why always null???
                                if (oldmember == null)
                                {
                                    XmlElement members = MDocUpdater.WriteElement (basefile.DocumentElement, "Members");
                                    oldmember = basefile.CreateElement ("Member");
                                    oldmember.SetAttribute ("MemberName", dm.MemberName);
                                    members.AppendChild (oldmember);
                                    foreach (string key in MDocUpdater.Sort (dm.MemberSignatures.Keys))
                                    {
                                        XmlElement ms = basefile.CreateElement ("MemberSignature");
                                        ms.SetAttribute ("Language", key);
                                        ms.SetAttribute ("Value", (string)dm.MemberSignatures[key]);
                                        oldmember.AppendChild (ms);
                                    }
                                    oldmember.SetAttribute ("__monodocer-seen__", "true");
                                    Console.WriteLine ("Member Added: {0}", oldmember.SelectSingleNode ("MemberSignature[@Language='C#']/@Value").InnerText);
                                    app.additions++;
                                }
                            }
                            else
                            {
                                m = GetMember (type, new DocumentationMember (oldmember, typeEntry));
                                if (m == null)
                                {
                                    app.Warning ("Could not import ECMA docs for `{0}'s `{1}': Member not found.",
                                            type.FullName, dm.MemberSignatures["C#"]);
                                    continue;
                                }
                                oldmember.SetAttribute ("__monodocer-seen__", "true");
                            }
                            DocsNodeInfo node = new DocsNodeInfo (oldmember, m);
                            if (ecmadocs.Name != "Docs")
                                throw new InvalidOperationException ("Found " + ecmadocs.Name + "; expected <Docs/>!");
                            yield return node;
                            break;
                        }
                    case "Members":
                        if (membersDepth == ecmadocs.Depth && ecmadocs.NodeType == XmlNodeType.EndElement)
                        {
                            go = false;
                        }
                        break;
                }
            }
        }
    }
}