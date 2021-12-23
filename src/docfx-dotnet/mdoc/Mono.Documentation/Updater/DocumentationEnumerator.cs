using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Mono.Cecil;
using StringList = System.Collections.Generic.List<string>;

using Mono.Documentation.Util;
using Mono.Documentation.Updater.Frameworks;

namespace Mono.Documentation.Updater
{
    public class DocumentationEnumerator
    {

        public virtual IEnumerable<TypeDefinition> GetDocumentationTypes (AssemblyDefinition assembly, List<string> forTypes)
        {
            return GetDocumentationTypes (assembly, forTypes, null);
        }

        protected IEnumerable<TypeDefinition> GetDocumentationTypes (AssemblyDefinition assembly, List<string> forTypes, HashSet<string> seen)
        {
            foreach (TypeDefinition type in assembly.GetTypes ())
            {
                if (forTypes != null && forTypes.BinarySearch (type.FullName) < 0)
                    continue;
                if (seen != null && seen.Contains (type.FullName))
                    continue;
                yield return DocUtils.FixUnnamedParameters (type);
            }
        }

        public virtual IEnumerable<DocsNodeInfo> GetDocumentationMembers (XmlDocument basefile, TypeDefinition type, FrameworkTypeEntry typeEntry)
        {
            foreach (XmlElement oldmember in basefile.SelectNodes ("Type/Members/Member"))
            {
                if (oldmember.GetAttribute ("__monodocer-seen__") == "true")
                {
                    oldmember.RemoveAttribute ("__monodocer-seen__");
                    continue;
                }
                if (oldmember.ParentNode == null)
                    continue;
                
                MemberReference m = GetMember (type, new DocumentationMember (oldmember, typeEntry));
                if (m == null)
                {
                    yield return new DocsNodeInfo (oldmember);
                }
                else
                {
                    yield return new DocsNodeInfo (oldmember, m);
                }
            }
        }

        public static MemberReference GetMember (TypeDefinition type, DocumentationMember member)
        {
            string membertype = member.MemberType;

            string returntype = member.ReturnType;

            string docName = member.MemberName;

            string[] docTypeParams = GetTypeParameters (docName, member.TypeParameters);

            // If we're using 'magic types', then we might get false positives ... in those cases, we keep searching
            MemberReference likelyCandidate = null;

            // Loop through all members in this type with the same name
            var reflectedMembers = GetReflectionMembers (type, docName, membertype).ToArray ();
            foreach (MemberReference mi in reflectedMembers)
            {
                bool matchedMagicType = false;
                if (mi is TypeDefinition) continue;
                if (MDocUpdater.GetMemberType (mi) != membertype) continue;

                if (MDocUpdater.IsPrivate (mi))
                    continue;

                IList<ParameterDefinition> pis = null;
                string[] typeParams = null;
                if (mi is MethodDefinition)
                {
                    MethodDefinition mb = (MethodDefinition)mi;
                    pis = mb.Parameters;
                    if (mb.IsGenericMethod ())
                    {
                        IList<GenericParameter> args = mb.GenericParameters;
                        typeParams = args.Select (p => p.Name).ToArray ();
                    }
                }
                else if (mi is PropertyDefinition)
                    pis = ((PropertyDefinition)mi).Parameters;

                // check type parameters
                int methodTcount = member.TypeParameters == null ? 0 : member.TypeParameters.Count;
                int reflectionTcount = typeParams == null ? 0 : typeParams.Length;
                if (methodTcount != reflectionTcount)
                    continue;

                // check member parameters
                int mcount = member.Parameters == null ? 0 : member.Parameters.Count;
                int pcount = pis == null ? 0 : pis.Count;
                if (mcount != pcount)
                    continue;

                MethodDefinition mDef = mi as MethodDefinition;
                if (mDef != null && !mDef.IsConstructor && (mDef.Name.StartsWith("op_Explicit", StringComparison.Ordinal) || mDef.Name.StartsWith("op_Implicit", StringComparison.Ordinal))) 
                {
                    // Casting operators can overload based on return type.
                    string rtype = GetReplacedString (
                                       MDocUpdater.GetDocTypeFullName (((MethodDefinition)mi).ReturnType),
                                       typeParams, docTypeParams);
                    string originalRType = rtype;
                    if (MDocUpdater.SwitchingToMagicTypes)
                    {
                        rtype = NativeTypeManager.ConvertFromNativeType (rtype);

                    }
                    if ((returntype != rtype && originalRType == rtype) ||
                        (MDocUpdater.SwitchingToMagicTypes && returntype != originalRType && returntype != rtype && originalRType != rtype))
                    {
                        continue;
                    }

                    if (originalRType != rtype)
                        matchedMagicType = true;
                }

                if (pcount == 0)
                    return mi;
                bool isExtensionMethod = DocUtils.IsExtensionMethod(mDef);
                bool good = true;
                for (int i = 0; i < pis.Count; i++)
                {
                    bool isRefType = pis[i].ParameterType is ByReferenceType;

                    if (i == 0 && !isRefType && isExtensionMethod)
                        isRefType = true; // this will be the case for generic parameter types

                    string paramType = GetReplacedString (
                        MDocUpdater.GetDocParameterType (pis[i].ParameterType),
                        typeParams, docTypeParams);

                    // if magictypes, replace paramType to "classic value" ... so the comparison works
                    string originalParamType = paramType;
                    if (MDocUpdater.SwitchingToMagicTypes)
                    {
                        paramType = NativeTypeManager.ConvertFromNativeType (paramType);
                    }

                    string xmlMemberType = member.Parameters[i];

                    // TODO: take into account extension method reftype
                    bool xmlIsRefType = xmlMemberType.Contains ('&');
                    bool refTypesMatch = isRefType == xmlIsRefType;

                    if (!refTypesMatch) {
                        good = false;
                        break;
                    }

                    xmlMemberType = xmlIsRefType ? xmlMemberType.Substring (0, xmlMemberType.Length - 1) : xmlMemberType;

                    if ((!paramType.Equals (xmlMemberType) && paramType.Equals (originalParamType)) ||
                        (MDocUpdater.SwitchingToMagicTypes && !originalParamType.Equals (xmlMemberType) && !paramType.Equals (xmlMemberType) && !paramType.Equals (originalParamType)))
                    {

                        // did not match ... if we're dropping the namespace, and the paramType has the dropped
                        // namespace, we should see if it matches when added
                        bool stillDoesntMatch = true;
                        if (MDocUpdater.HasDroppedNamespace (type) && paramType.StartsWith (MDocUpdater.droppedNamespace))
                        {
                            string withDroppedNs = string.Format ("{0}.{1}", MDocUpdater.droppedNamespace, xmlMemberType);

                            stillDoesntMatch = withDroppedNs != paramType;
                        }

                        if (stillDoesntMatch)
                        {
                            good = false;
                            break;
                        }
                    }

                    if (originalParamType != paramType)
                        matchedMagicType = true;
                    
                }
                if (!good) continue;

                if (MDocUpdater.SwitchingToMagicTypes && likelyCandidate == null && matchedMagicType)
                {
                    // we matched this on a magic type conversion ... let's keep going to see if there's another one we should look at that matches more closely
                    likelyCandidate = mi;
                    continue;
                }

                return mi;
            }

            return likelyCandidate;
        }

        static string[] GetTypeParameters (string docName, IEnumerable<string> knownParameters)
        {
            if (docName[docName.Length - 1] != '>')
                return null;
            StringList types = new StringList ();
            int endToken = docName.Length - 2;
            int i = docName.Length - 2;
            do
            {
                if (docName[i] == ',' || docName[i] == '<')
                {
                    types.Add (docName.Substring (i + 1, endToken - i));
                    endToken = i - 1;
                }
                if (docName[i] == '<')
                    break;
            } while (--i >= 0);

            types.Reverse ();
            var arrayTypes = types.ToArray ();

            if (knownParameters != null && knownParameters.Any () && arrayTypes.Length != knownParameters.Count ())
                return knownParameters.ToArray ();
            else
                return arrayTypes;
        }

        public static IEnumerable<MemberReference> GetReflectionMembers (TypeDefinition type, string docName, string memberType)
        {
            return GetReflectionMembersCore (type, docName, memberType)
                .Distinct ();
        }

        private static IEnumerable<MemberReference> GetReflectionMembersCore (TypeDefinition type, string docName, string memberType)
        {
            // In case of dropping the namespace, we have to remove the dropped NS
            // so that docName will match what's in the assembly/type
            if (MDocUpdater.HasDroppedNamespace (type) && docName.StartsWith (MDocUpdater.droppedNamespace + "."))
            {
                int droppedNsLength = MDocUpdater.droppedNamespace.Length;
                docName = docName.Substring (droppedNsLength + 1, docName.Length - droppedNsLength - 1);
            }



            // need to worry about 4 forms of //@MemberName values:
            //  1. "Normal" (non-generic) member names: GetEnumerator
            //    - Lookup as-is.
            //  2. Explicitly-implemented interface member names: System.Collections.IEnumerable.Current
            //    - try as-is, and try type.member (due to "kludge" for property
            //      support.
            //  3. "Normal" Generic member names: Sort<T> (CSC)
            //    - need to remove generic parameters --> "Sort"
            //  4. Explicitly-implemented interface members for generic interfaces: 
            //    -- System.Collections.Generic.IEnumerable<T>.Current
            //    - Try as-is, and try type.member, *keeping* the generic parameters.
            //     --> System.Collections.Generic.IEnumerable<T>.Current, IEnumerable<T>.Current
            //  5. As of 2008-01-02, gmcs will do e.g. 'IFoo`1[A].Method' instead of
            //    'IFoo<A>.Method' for explicitly implemented methods; don't interpret
            //    this as (1) or (2).
            if (docName.IndexOf ('<') == -1 && docName.IndexOf ('[') == -1)
            {
                int memberCount = 0;

                // Cases 1 & 2
                foreach (MemberReference mi in type.GetMembers (docName))
                {
                    memberCount++;
                    yield return mi;
                }

                if (memberCount == 0 && CountChars (docName, '.') > 0)
                {

                    Func<MemberReference, bool> verifyInterface = (member) =>
                    {
                        var meth = member as MethodDefinition;

                        if (meth == null && member is PropertyReference)
                        {
                            var propertyDefinition = ((PropertyReference)member).Resolve ();
                            meth = propertyDefinition.GetMethod ?? propertyDefinition.SetMethod;
                        }
                        return meth != null && (member.Name.Equals (".ctor") || DocUtils.IsExplicitlyImplemented (meth));
                    };


                    // might be a property; try only type.member instead of
                    // namespace.type.member.
                    var typeMember = DocUtils.GetTypeDotMember (docName);
                    var memberName = DocUtils.GetMember (docName);
                    foreach (MemberReference mi in
                         type.GetMembers (typeMember).Where (verifyInterface))
                    {
                        memberCount++;
                        yield return mi;
                    }

                    // some VB libraries use just the member name
                    foreach (MemberReference mi in
                         type.GetMembers (memberName).Where (verifyInterface))
                    {
                        memberCount++;
                        yield return mi;
                    }

                    // some VB libraries use a `typemember` naming convention
                    foreach (MemberReference mi in
                         type.GetMembers (typeMember.Replace (".", "")).Where (verifyInterface))
                    {
                        memberCount++;
                        yield return mi;
                    }

                    // if we still haven't found the member, there are some VB libraries
                    // that use a different interface name for implementation. 
                    if (memberCount == 0)
                    {
                        foreach (MemberReference mi in
                            type
                                .GetMembers()
                                .Where(m => m.Name.StartsWith("I", StringComparison.InvariantCultureIgnoreCase) &&
                                            m.Name.EndsWith(memberName, StringComparison.InvariantCultureIgnoreCase))
                                .Where(verifyInterface))
                        {
                            memberCount++;
                            yield return mi;
                        }
                    }

                    if (memberCount == 0 && memberType == "Property")
                    {
                        foreach (MemberReference mr in type.GetMembers().Where(x => x is PropertyDefinition))
                        {
                            var method = ((PropertyDefinition) mr).GetMethod ?? ((PropertyDefinition) mr).SetMethod;
                            if (method?.Overrides != null && method.Overrides.Any())
                            {
                                DocUtils.GetInfoForExplicitlyImplementedMethod(method, out TypeReference iface, out MethodReference ifaceMethod);
                                var newName = DocUtils.GetMemberForProperty(ifaceMethod.Name);
                                if (newName == memberName && verifyInterface(mr) && docName.Contains (iface.Name))
                                    yield return mr;
                            }
                        }
                    }
                }
                yield break;
            }
            // cases 3 & 4
            int numLt = 0;
            int numDot = 0;
            int startLt, startType, startMethod;
            startLt = startType = startMethod = -1;
            for (int i = 0; i < docName.Length; ++i)
            {
                switch (docName[i])
                {
                    case '<':
                        if (numLt == 0)
                        {
                            startLt = i;
                        }
                        ++numLt;
                        break;
                    case '>':
                        --numLt;
                        if (numLt == 0 && (i + 1) < docName.Length)
                            // there's another character in docName, so this <...> sequence is
                            // probably part of a generic type -- case 4.
                            startLt = -1;
                        break;
                    case '.':
                        startType = startMethod;
                        startMethod = i;
                        ++numDot;
                        break;
                }
            }
            string refName = startLt == -1 ? docName : docName.Substring (0, startLt);
            // case 3
            foreach (MemberReference mi in type.GetMembers (refName))
                yield return mi;

            // case 4
            foreach (MemberReference mi in type.GetMembers (refName.Substring (startType + 1)))
                yield return mi;

            // If we _still_ haven't found it, we've hit another generic naming issue:
            // post Mono 1.1.18, gmcs generates [[FQTN]] instead of <TypeName> for
            // explicitly-implemented METHOD names (not properties), e.g. 
            // "System.Collections.Generic.IEnumerable`1[[Foo, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]].GetEnumerator"
            // instead of "System.Collections.Generic.IEnumerable<Foo>.GetEnumerator",
            // which the XML docs will contain.
            //
            // Alas, we can't derive the Mono name from docName, so we need to iterate
            // over all member names, convert them into CSC format, and compare... :-(
            if (numDot == 0)
                yield break;
            foreach (MemberReference mi in type.GetMembers ())
            {
                if (MDocUpdater.GetMemberName (mi) == docName)
                    yield return mi;
            }
        }

        static string GetReplacedString (string typeName, string[] from, string[] to)
        {
            if (from == null)
                return typeName;
            for (int i = 0; i < from.Length; ++i)
                typeName = typeName.Replace (from[i], to[i]);
            return typeName;
        }

        private static int CountChars (string s, char c)
        {
            int count = 0;
            for (int i = 0; i < s.Length; ++i)
            {
                if (s[i] == c)
                    ++count;
            }
            return count;
        }
    }
}
