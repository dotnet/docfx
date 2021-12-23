using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Mono.Cecil;
using Mono.Documentation.Updater.Frameworks;

namespace Mono.Documentation.Updater
{
    public static class XmlSyncer
    {
        public static void MakeParameters (XmlElement root, MemberReference member, IList<ParameterDefinition> parameters, FrameworkTypeEntry typeEntry, ref bool fxAlternateTriggered)
        {
            XmlElement e = DocUtils.WriteElement (root, "Parameters");

            /// addParameter does the work of adding the actual parameter to the XML
            Action<ParameterDefinition, XmlElement, string, int, bool, string, bool> addParameter = (ParameterDefinition param, XmlElement nextTo, string paramType, int index, bool addIndex, string fx, bool addfx) =>
            {
                var pe = root.OwnerDocument.CreateElement ("Parameter");

                if (nextTo == null)
                    e.AppendChild (pe);
                else
                    e.InsertAfter (pe, nextTo);

                pe.SetAttribute ("Name", param.Name);
                pe.SetAttribute ("Type", paramType);
                if (param.ParameterType is ByReferenceType)
                {
                    if (param.IsOut)
                        pe.SetAttribute ("RefType", "out");
                    else
                        pe.SetAttribute ("RefType", "ref");
                }
                if (addIndex)
                    pe.SetAttribute ("Index", index.ToString ());
                if (addfx)
					pe.SetAttribute (Consts.FrameworkAlternate, fx);

                MakeAttributes (pe, GetCustomAttributes (param.CustomAttributes, ""));
            };

            /// addFXAttributes, adds the index attribute to all existing elements.
            /// Used when we first detect the scenario which requires this.
            Action<XmlNodeList> addFXAttributes = nodes =>
            {
                var i = 0;
                foreach (var node in nodes.Cast<XmlElement> ())
                {
                    node.SetAttribute ("Index", i.ToString ());
                    i++;
                }
            };

            int parameterIndex = 0;
            int parameterIndexOffset = 0;

            var paramNodes = e.GetElementsByTagName ("Parameter");
            bool inFXMode = frameworksCache.Frameworks.Count () > 1;

            foreach (ParameterDefinition p in parameters)
            {
                var ptype = GetDocParameterType (p.ParameterType);
                if (parameterIndex >= paramNodes.Count)
                {
                    // this parameter hasn't been added yet
                    bool hasParameterName = string.IsNullOrWhiteSpace (p.Name);
                    addParameter (p, null, ptype, parameterIndex, false, "", false);
                }
                else // there's enough nodes, see if it truly exists
                {
                    //look for < parameter > that matches position
                    XmlElement parameterNode = e.ChildNodes[parameterIndex + parameterIndexOffset] as XmlElement;


                    if (parameterNode != null)
                    {
                        //Assert Type Matches (if not, throw?)
                        if (parameterNode.HasAttribute ("Name") && parameterNode.Attributes["Name"].Value == p.Name)
                        {
                            // we're good, continue on.
                        }
                        else
                        { // name doesn't match
                            if (parameterNode.HasAttribute ("Index"))
                            {
                                // TODO: add a FrameworkAlternate check, and set offset correctly
                                int pindex;
                                if (int.TryParse (parameterNode.GetAttribute ("Index"), out pindex) && pindex < parameterIndex)
                                {
                                    parameterIndexOffset++;

                                    continue;
                                }
                            }
                            else
                            {
                                if (!inFXMode) throw new Exception ("shit");
                                addFXAttributes (paramNodes);
                                //-find type in previous frameworks


                                string fxList = FXUtils.PreviouslyProcessedFXString (typeEntry);

                                //-find < parameter where index = currentIndex >
                                var currentNode = paramNodes[parameterIndex] as XmlElement;
                                currentNode.SetAttribute (Consts.FrameworkAlternate, fxList);

                                addParameter (p, parameterNode, ptype, parameterIndex - parameterIndexOffset, true, typeEntry.Framework.Name, true);
                                parameterIndexOffset++;
                                fxAlternateTriggered = true;
                            }
                        }

                    }
                    else
                    { // no element at this index
                        // TODO: does this ever happen?
                        throw new Exception ("This wasn't supposed to happen");
                        //addParameter (p);
                    }
                    /*
                    - If found
                       - Assert Type Matches (if not, throw?)
                        -If Name Matches … 
                            - if “FrameworkAlternate” 
                                -Add typeEntry.Framework.Name to list
                           - done!
                       -Else (exists, but name doesn’t match … FrameworkAlternate path)
                           - check if inFXMode if not, throw
                           -AddFXParameters
                               - adds Index to all existing<parameters
                                -find type in previous frameworks
                                -find < parameter where index = currentIndex >
                                -Add FrameworkAlternate = allPreviousFrameworks and Index = currentIndex
                           - Add new node with Index = currentIndex
                   - else not found
                        -add
                                */
                }
                parameterIndex++;
            }
            //-purge `typeEntry.Framework` from any<parameter> that 
            // has FrameworkAlternate, and “name” doesn’t match any 
            // `parameters`
            var alternates = paramNodes
                .Cast<XmlElement> ()
                .Select (p => new
                {
                    Element = p,
                    Name = p.GetAttribute ("Name"),
                    FrameworkAlternate = p.GetAttribute (Consts.FrameworkAlternate)
                })
                .Where (p =>
                        !string.IsNullOrWhiteSpace (p.FrameworkAlternate) &&
                        p.FrameworkAlternate.Contains (typeEntry.Framework.Name) &&
                        !parameters.Any (param => param.Name == p.Name))
                .ToArray ();
            if (alternates.Any ())
            {
                foreach (var a in alternates)
                {
                    string newValue = FXUtils.RemoveFXFromList (a.FrameworkAlternate, typeEntry.Framework.Name);
                    if (string.IsNullOrWhiteSpace (newValue))
                    {
                        a.Element.RemoveAttribute (Consts.FrameworkAlternate);
                    }
                    else
                    {
                        a.Element.SetAttribute (Consts.FrameworkAlternate, newValue);
                    }
                }
            }

            return;
            /*
            // old code
            foreach (ParameterDefinition p in parameters)
            {
                XmlElement pe;

                // param info
                var ptype = GetDocParameterType (p.ParameterType);
                var newPType = ptype;

                if (MDocUpdater.SwitchingToMagicTypes)
                {
                    newPType = NativeTypeManager.ConvertFromNativeType (ptype);
                }

                // now find the existing node, if it's there so we can reuse it.
                var nodes = root.SelectSingleNode ("Parameters").SelectNodes ("Parameter")
                    .Cast<XmlElement> ().Where (x => x.GetAttribute ("Name") == p.Name)
                    .ToArray ();

                // FYI: Exists? No?
                if (nodes.Count () == 0)
                {
                    // TODO: instead of this. Needs to be replaced with a better
                    // check for Parameter index ... should I add parameter index?

                    // are we in frameworks mode?
                    // add Index to all existing parameter nodes if they don't have them
                    // match existing to position and type
                    bool _inFXMode = typeEntry.Framework.Frameworks.Count () > 1;

                    // when I find the one, name won't match ... 

                    //  find all "previous" frameworks
                    //  Add FrameworkAlternate with previous frameworks to found/pre-existing node
                    var allPreviousTypes_ = typeEntry.Framework.Frameworks
                                            .Where (f => f.index < typeEntry.Framework.index)
                                            .Select (f => f.FindTypeEntry (typeEntry))
                                            .ToArray ();

                    var allPreviousFrameworks = allPreviousTypes.Value.Select (previous => previous.Framework.Name).ToArray ();
                    string fxList = string.Join (";", allPreviousFrameworks);

                    // find the parameters in `root` that have an index == this parameter's index
                    // if they don't match, then we need to make a new one for this

                    // Create new "Parameter" node, with FrameworkAlternate = this

                    // Legacy: wasn't found, let's make sure it wasn't just cause the param name was changed
                    nodes = root.SelectSingleNode ("Parameters").SelectNodes ("Parameter")
                        .Cast<XmlElement> ()
                        .Skip (parameterIndex) // this makes sure we don't inadvertently "reuse" nodes when adding new ones
                        .Where (x => x.GetAttribute ("Name") != p.Name && (x.GetAttribute ("Type") == ptype || x.GetAttribute ("Type") == newPType))
                        .Take (1) // there might be more than one that meets this parameter ... only take the first.
                        .ToArray ();
                }

                AddXmlNode (nodes,
                    x => x.GetAttribute ("Type") == ptype,
                    x => x.SetAttribute ("Type", ptype),
                    () =>
                    {
                        pe = root.OwnerDocument.CreateElement ("Parameter");
                        e.AppendChild (pe);

                        pe.SetAttribute ("Name", p.Name);
                        pe.SetAttribute ("Type", ptype);
                        if (p.ParameterType is ByReferenceType)
                        {
                            if (p.IsOut)
                                pe.SetAttribute ("RefType", "out");
                            else
                                pe.SetAttribute ("RefType", "ref");
                        }

                        MakeAttributes (pe, GetCustomAttributes (p.CustomAttributes, ""));
                        return pe;
                    },
                    member);

                parameterIndex++;
            }

            // TODO: was there a `Parameter` that we didn't process that has FrameworkAlternate?
            // if yes, remove this framework from that FrameworkAlternate
            // if that makes the list empty, remove the node and corresponding /Docs/parameter node
            */
        }




    }
}
