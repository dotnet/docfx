using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Mono.Cecil;
using Mono.Collections.Generic;
using Mono.Documentation.Updater.Frameworks;
using Mono.Documentation.Util;

namespace Mono.Documentation.Updater
{
    public static class DocUtils
    {

        public static void AddElementWithFx(FrameworkTypeEntry typeEntry, XmlElement parent, bool isFirst, bool isLast, Lazy<string> allfxstring, Action<XmlElement> clear, Func<XmlElement, XmlElement> findExisting, Func<XmlElement, XmlElement> addItem)
        {
            if (typeEntry != null && typeEntry.TimesProcessed > 1)
                return;

            if (isFirst)
            {
                clear(parent);
            }

            var item = findExisting(parent);

            if (item == null)
            {
                item = addItem(parent);
            }

            if (typeEntry != null)
            {
                item.AddFrameworkToElement(typeEntry.Framework);
            }
            
            if (isLast)
            {
                item.ClearFrameworkIfAll(allfxstring.Value);
            }
        }
        public static void ClearFrameworkIfAll(this XmlElement element, string allfxstring)
        {
            if (element.HasAttribute(Consts.FrameworkAlternate) && element.GetAttribute(Consts.FrameworkAlternate) == allfxstring)
            {
                element.RemoveAttribute(Consts.FrameworkAlternate);
            }
        }

        public static void AddFrameworkToElement(this XmlElement element, FrameworkEntry framework)
        {
            var fxaValue = FXUtils.AddFXToList(element.GetAttribute(Consts.FrameworkAlternate), framework.Name);

            element.SetAttribute(Consts.FrameworkAlternate, fxaValue);
        }

        public static bool DoesNotHaveApiStyle (this XmlElement element, ApiStyle style)
        {
            string styleString = style.ToString ().ToLowerInvariant ();
            string apistylevalue = element.GetAttribute ("apistyle");
            return apistylevalue != styleString || string.IsNullOrWhiteSpace (apistylevalue);
        }
        public static bool HasApiStyle (this XmlElement element, ApiStyle style)
        {
            string styleString = style.ToString ().ToLowerInvariant ();
            return element.GetAttribute ("apistyle") == styleString;
        }
        public static bool HasApiStyle (this XmlNode node, ApiStyle style)
        {
            var attribute = node.Attributes["apistyle"];
            return attribute != null && attribute.Value == style.ToString ().ToLowerInvariant ();
        }
        public static void AddApiStyle (this XmlElement element, ApiStyle style)
        {
            string styleString = style.ToString ().ToLowerInvariant ();
            var existingValue = element.GetAttribute ("apistyle");
            if (string.IsNullOrWhiteSpace (existingValue) || existingValue != styleString)
            {
                element.SetAttribute ("apistyle", styleString);
            }

            // Propagate the API style up to the membernode if necessary
            if (element.LocalName == "AssemblyInfo" && element.ParentNode != null && element.ParentNode.LocalName == "Member")
            {
                var member = element.ParentNode;
                var unifiedAssemblyNode = member.SelectSingleNode ("AssemblyInfo[@apistyle='unified']");
                var classicAssemblyNode = member.SelectSingleNode ("AssemblyInfo[not(@apistyle) or @apistyle='classic']");

                var parentAttribute = element.ParentNode.Attributes["apistyle"];
                Action removeStyle = () => element.ParentNode.Attributes.Remove (parentAttribute);
                Action propagateStyle = () =>
                {
                    if (parentAttribute == null)
                    {
                        // if it doesn't have the attribute, then add it
                        parentAttribute = element.OwnerDocument.CreateAttribute ("apistyle");
                        parentAttribute.Value = styleString;
                        element.ParentNode.Attributes.Append (parentAttribute);
                    }
                };

                if ((style == ApiStyle.Classic && unifiedAssemblyNode != null) || (style == ApiStyle.Unified && classicAssemblyNode != null))
                    removeStyle ();
                else
                    propagateStyle ();
            }
        }
        public static string GetFormattedTypeName(string name)
        {
            int index = name.IndexOf("`", StringComparison.Ordinal);
            if (index >= 0)
                return name.Substring(0, index);

            return name;
        }
        public static void AddApiStyle (this XmlNode node, ApiStyle style)
        {
            string styleString = style.ToString ().ToLowerInvariant ();
            var existingAttribute = node.Attributes["apistyle"];
            if (existingAttribute == null)
            {
                existingAttribute = node.OwnerDocument.CreateAttribute ("apistyle");
                node.Attributes.Append (existingAttribute);
            }
            existingAttribute.Value = styleString;
        }
        public static void RemoveApiStyle (this XmlElement element, ApiStyle style)
        {
            string styleString = style.ToString ().ToLowerInvariant ();
            string existingValue = element.GetAttribute ("apistyle");
            if (string.IsNullOrWhiteSpace (existingValue) || existingValue == styleString)
            {
                element.RemoveAttribute ("apistyle");
            }
        }
        public static void RemoveApiStyle (this XmlNode node, ApiStyle style)
        {
            var styleAttribute = node.Attributes["apistyle"];
            if (styleAttribute != null && styleAttribute.Value == style.ToString ().ToLowerInvariant ())
            {
                node.Attributes.Remove (styleAttribute);
            }
        }

        public static IEnumerable<T> SafeCast<T> (this System.Collections.IEnumerable list)
        {
            if (list == null) yield break;

            foreach (object item in list)
            {
                if (item is T castedItem)
                {
                    yield return castedItem;
                }
            }
        }

        public static bool IsExplicitlyImplemented (MethodDefinition method)
        {
            return method != null && method.IsPrivate && method.IsFinal && method.IsVirtual;
        }

        public static string GetTypeDotMember (string name)
        {
            int startType, startMethod;
            startType = startMethod = -1;
            for (int i = 0; i < name.Length; ++i)
            {
                if (name[i] == '.')
                {
                    startType = startMethod;
                    startMethod = i;
                }
            }
            return name.Substring (startType + 1);
        }

        public static string GetMember(string name)
        {
            int i = name.LastIndexOf('.');
            var memberName = i == -1 ? name : name.Substring(i + 1);

            return memberName;
        }

        public static string GetMemberForProperty(string name)
        {
            int i = name.LastIndexOf('.');
            var memberName = i == -1 ? name : name.Substring(i + 1);

            if (memberName.StartsWith("get_") || memberName.StartsWith("set_") || memberName.StartsWith("put_"))
            {
                var index = memberName.IndexOf("_", StringComparison.InvariantCulture);
                if (index > 0)
                    //remove get/set prefix from method name
                    memberName = memberName.Substring(index + 1);
            }

            return memberName;
        }

        public static void GetInfoForExplicitlyImplementedMethod (
                MethodDefinition method, out TypeReference iface, out MethodReference ifaceMethod)
        {
            iface = null;
            ifaceMethod = null;
            if (method.Overrides.Count != 1)
                throw new InvalidOperationException ("Could not determine interface type for explicitly-implemented interface member " + method.Name);
            iface = method.Overrides[0].DeclaringType;
            ifaceMethod = method.Overrides[0];
        }

        public static bool IsPublic (TypeDefinition type)
        {
            TypeDefinition decl = type;
            while (decl != null)
            {
                if (!(decl.IsPublic || decl.IsNestedPublic ||
                            decl.IsNestedFamily || decl.IsNestedFamily || decl.IsNestedFamilyOrAssembly))
                {
                    return false;
                }
                decl = (TypeDefinition)decl.DeclaringType;
            }
            return true;
        }

        public static string GetPropertyName (PropertyDefinition pi, string delimeter = ".")
        {
            // Issue: (g)mcs-generated assemblies that explicitly implement
            // properties don't specify the full namespace, just the 
            // TypeName.Property; .NET uses Full.Namespace.TypeName.Property.
            MethodDefinition method = pi.GetMethod ?? pi.SetMethod;
            bool isExplicitlyImplemented = IsExplicitlyImplemented(method);
            if (!isExplicitlyImplemented)
                return pi.Name;

            // Need to determine appropriate namespace for this member.
            GetInfoForExplicitlyImplementedMethod (method, out var iface, out var ifaceMethod);
            var stringifyIface = DocTypeFullMemberFormatter.Default.GetName(iface).Replace(".", delimeter);
            return string.Join (delimeter, new string[]{
                stringifyIface,
                GetMemberForProperty (ifaceMethod.Name)});
        }

        public static string GetNamespace (TypeReference type, string delimeter = null)
        {
            if (type == null)
                return string.Empty;

            if (type.GetElementType ().IsNested)
                type = type.GetElementType ();
            while (type != null && type.IsNested && !type.IsGenericParameter)
                type = type.DeclaringType;
            if (type == null)
                return string.Empty;

            string typeNS = type.Namespace;

            if (!string.IsNullOrEmpty(delimeter))
            {
                typeNS = typeNS.Replace(".", delimeter);
            }

            // first, make sure this isn't a type reference to another assembly/module

            bool isInAssembly = MDocUpdater.IsInAssemblies (type.Module.Name);
            if (isInAssembly && !typeNS.StartsWith ("System") && MDocUpdater.HasDroppedNamespace (type))
            {
                typeNS = string.Format ("{0}{1}{2}", MDocUpdater.droppedNamespace, delimeter ?? ".", typeNS);
            }
            return typeNS;
        }

        public static string PathCombine (string dir, string path)
        {
            if (dir == null)
                dir = "";
            if (path == null)
                path = "";
            return Path.Combine (dir, path);
        }

        public static bool IsExtensionMethod (MethodDefinition method)
        {
            if (method == null) return false;

            return
                method.CustomAttributes
                        .Any (m => m.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute")
                && method.DeclaringType.CustomAttributes
                        .Any (m => m.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute");
        }

        public static bool IsDelegate (TypeDefinition type)
        {
            TypeReference baseRef = type.BaseType;
            if (baseRef == null)
                return false;
            return !type.IsAbstract && baseRef.FullName == "System.Delegate" || // FIXME
                    baseRef.FullName == "System.MulticastDelegate";
        }

        public static bool ClearNodesIfNotDefault(XmlNode n, XmlNode incoming, int depth = 0)
        {
            if (n is XmlText && n.InnerText == "To Be Added.")
                return false;
            else if (n is XmlComment)
                return false;
            else
            {
                bool removed = true;
                foreach (var nchild in n.ChildNodes.Cast<XmlNode>().ToArray())
                {
                    if (nchild == null) continue;

                    if (nchild is XmlComment || nchild is XmlText || nchild is XmlCDataSection)
                    {
                        nchild.ParentNode.RemoveChild(nchild);
                        removed = true;
                        continue;
                    }

                    if (depth == 0)
                    {
                        // check the first level children to see if there's an incoming node that matches
                        var avalues = nchild.Attributes?.Cast<XmlAttribute>().Select(a => $"@{a.Name}='{a.Value}'").ToArray();
                        var nodexpath = $"./{nchild.Name}";
                        if (avalues?.Length > 0)
                            nodexpath += $"[{ string.Join(" and ", avalues) }]";

                        XmlNode incomingEquivalent;

                        try
                        {
                            incomingEquivalent = incoming.SelectSingleNode(nodexpath);
                        }
                        catch (XPathException xex)
                        {
                            throw new MDocException($"xpath error: {nodexpath}. On incoming node {incoming.OuterXml}", xex);
                        }

                        if (incomingEquivalent != null)
                        {
                            nchild.ParentNode.RemoveChild(nchild);
                            removed = true;
                        }
                    }
                    else if (ClearNodesIfNotDefault(nchild, incoming, depth + 1) && depth == 1)
                    {
                        nchild.ParentNode.RemoveChild(nchild);
                        removed = true;
                    }
                    else
                    {
                        removed = false;
                    }
                }
                if (removed) return true;
            }

            return false;

        }

        public static bool NeedsOverwrite(XmlElement element)
        {
            return element != null &&
                   !(element.HasAttribute("overwrite") &&
                    element.Attributes["overwrite"].Value.Equals("false", StringComparison.InvariantCultureIgnoreCase));
        }

        public static List<TypeReference> GetDeclaringTypes (TypeReference type)
        {
            List<TypeReference> decls = new List<TypeReference> ();
            decls.Add (type);
            while (type.DeclaringType != null)
            {
                decls.Add (type.DeclaringType);
                type = type.DeclaringType;
            }
            decls.Reverse ();
            return decls;
        }

        public static int GetGenericArgumentCount (TypeReference type)
        {
            GenericInstanceType inst = type as GenericInstanceType;
            return inst != null
                    ? inst.GenericArguments.Count
                    : type.GenericParameters.Count;
        }

        class TypeEquality : IEqualityComparer<TypeReference>
        {
            bool IEqualityComparer<TypeReference>.Equals (TypeReference x, TypeReference y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;
                return x.FullName == y.FullName;
            }

            int IEqualityComparer<TypeReference>.GetHashCode (TypeReference obj)
            {
                return obj.GetHashCode ();
            }
        }
        static TypeEquality typeEqualityComparer = new TypeEquality ();

        public static IEnumerable<TypeReference> GetAllPublicInterfaces (TypeDefinition type)
        {
            return GetAllInterfacesFromType (type)
                .Where (i => IsPublic (i.Resolve ()))
                .Distinct (typeEqualityComparer);
        }

        private static IEnumerable<TypeReference> GetAllInterfacesFromType(TypeDefinition type)
        {
            if (type == null)
                yield break;

            foreach(var i in type.Interfaces)
            {
                yield return i.InterfaceType;
                foreach(var ii in GetAllInterfacesFromType(i.InterfaceType.Resolve()))
                {
                    yield return ii;
                }
            }
        }

        public static IEnumerable<TypeReference> GetUserImplementedInterfaces (TypeDefinition type)
        {
            if (!type.HasInterfaces)
                return new TypeReference[0];

            if (!Consts.CollapseInheritedInterfaces)
                return type.Interfaces.Select(i=> i.InterfaceType.Resolve()).Where(i => IsPublic (i));

            HashSet<string> inheritedInterfaces = GetInheritedInterfaces (type);

            List<TypeReference> userInterfaces = new List<TypeReference> ();
            foreach (var ii in type.Interfaces)
            {
                var iface = ii.InterfaceType;
                TypeReference lookup = iface.Resolve () ?? iface;

                var iname = GetQualifiedTypeName(lookup);

                if (!inheritedInterfaces.Contains (iname))
                    userInterfaces.Add (iface);
            }
            return userInterfaces.Where (i => IsPublic (i.Resolve ()));
        }

        private static string GetQualifiedTypeName (TypeReference type)
        {
            return "[" + type.Scope.Name + "]" + type.FullName;
        }

        private static HashSet<string> GetInheritedInterfaces (TypeDefinition type)
        {

            HashSet<string> inheritedInterfaces = new HashSet<string> ();

            Action<TypeDefinition> a = null;
            a = t =>
            {
                if (t == null) return;
                foreach (var r in t.Interfaces)
                {
                    var iname = GetQualifiedTypeName(r.InterfaceType);
                    inheritedInterfaces.Add (iname);
                    a (r.InterfaceType.Resolve ());
                }
            };
            TypeReference baseRef = type.BaseType;
            while (baseRef != null)
            {
                TypeDefinition baseDef = baseRef.Resolve ();
                if (baseDef != null)
                {
                    a (baseDef);
                    baseRef = baseDef.BaseType;
                }
                else
                    baseRef = null;
            }
            foreach (var r in type.Interfaces)
                a (r.InterfaceType.Resolve ());
            return inheritedInterfaces;
        }
        
        public static void AppendFieldValue(StringBuilder buf, FieldDefinition field)
        {
            // enums have a value__ field, which we ignore
            if (((TypeDefinition)field.DeclaringType).IsEnum ||
                    field.DeclaringType.IsGenericType())
                return;
            if (field.HasConstant && field.IsLiteral)
            {
                object val = null;
                try
                {
                    val = field.Constant;
                }
                catch
                {
                    return;
                }
                if (val == null)
                    buf.Append(" = ").Append("null");
                else if (val is Enum)
                    buf.Append(" = ").Append(val.ToString());
                else if (val is IFormattable)
                {
                    string value = null;
                    switch (field.FieldType.FullName)
                    {
                        case "System.Double":                          
                        case "System.Single":
                            value = ((IFormattable)val).ToString("R", CultureInfo.InvariantCulture);
                            break;
                        default:
                            value = ((IFormattable)val).ToString(null, CultureInfo.InvariantCulture);
                            break;
                    }
                    if (val is string)
                        value = "\"" + value + "\"";
                    buf.Append(" = ").Append(value);
                }
            }
        }

        /// <summary>
        /// XPath is invalid if it containt '-symbol inside '...'. 
        /// So, put string which contains '-symbol inside "...", and vice versa
        /// </summary>
        public static string GetStringForXPath(string input)
        {
            if (!input.Contains("'"))
                return $"\'{input}\'";
            if (!input.Contains("\""))
                return $"\"{input}\"";
            return input;
        }

        /// <summary>
        /// No documentation for property/event accessors.
        /// </summary>
        public static bool IsIgnored(MemberReference mi)
        {
            if (IsCompilerGenerated(mi))
            {
                if (mi.Name.StartsWith("get_", StringComparison.Ordinal)) return true;
                if (mi.Name.StartsWith("set_", StringComparison.Ordinal)) return true;
                if (mi.Name.StartsWith("put_", StringComparison.Ordinal)) return true;
                if (mi.Name.StartsWith("add_", StringComparison.Ordinal)) return true;
                if (mi.Name.StartsWith("remove_", StringComparison.Ordinal)) return true;
                if (mi.Name.StartsWith("raise_", StringComparison.Ordinal)) return true;
            }

            return false;
        }

        private static bool IsCompilerGenerated(MemberReference mi)
        {
           IMemberDefinition memberDefinition = mi.Resolve();
            if (memberDefinition != null)
            {
                return memberDefinition.IsSpecialName
                   || memberDefinition.CustomAttributes.Any(i =>
                       i.AttributeType.FullName == Consts.CompilerGeneratedAttribute
                       || i.AttributeType.FullName == Consts.CompilationMappingAttribute
                   );
            }
            else
            {
                MDocUpdater.Instance.Warning($"IsIgnored->IsCompilerGenerated Unable to Resolve Member('{mi.FullName}')");
                return false;
            }
        }

    public static bool IsAvailablePropertyMethod(MethodDefinition method)
        {
            return method != null 
                && (IsExplicitlyImplemented(method) 
                || (!method.IsPrivate && !method.IsAssembly && !method.IsFamilyAndAssembly));
        }

        /// <summary>
        /// Get all members of implemented interfaces as Dictionary [fingerprint string] -> MemberReference
        /// </summary>
        public static Dictionary<string, List<MemberReference>> GetImplementedMembersFingerprintLookup(TypeDefinition type)
        {
            var lookup = new Dictionary<string, List<MemberReference>>();
            List<TypeDefinition> previousInterfaces = new List<TypeDefinition>();
            foreach (var implementedInterface in type.Interfaces)
            {
                var interfaceType = implementedInterface.InterfaceType.Resolve();
                if (interfaceType == null)
                    continue;

                //Don't add duplicates of members which appear because of inheritance of interfaces
                bool addDuplicates = !previousInterfaces.Any(
                    i => i.Interfaces.Any(
                        j => j.InterfaceType.FullName == interfaceType.FullName));
                var genericInstanceType = implementedInterface.InterfaceType as GenericInstanceType;
                foreach (var memberReference in interfaceType.GetMembers())
                {
                    // pass genericInstanceType to resolve generic types if they are explicitly specified in the interface implementation code
                    var fingerprint = GetFingerprint(memberReference, genericInstanceType);
                    if (!lookup.ContainsKey(fingerprint))
                        lookup[fingerprint] = new List<MemberReference>();

                    // if it's going to be the first element with this fingerprint, add it anyway.
                    // otherwise, check addDuplicates flag
                    if (!lookup[fingerprint].Any() || addDuplicates)
                        lookup[fingerprint].Add(memberReference);
                }
                previousInterfaces.Add(interfaceType);
            }
            return lookup;
        }

        /// <summary>
        /// Get fingerprint of MemberReference. If fingerprints are equal, members can be implementations of each other
        /// </summary>
        /// <param name="memberReference">Type member which fingerprint is returned</param>
        /// <param name="genericInterface">GenericInstanceType instance to resolve generic types if they are explicitly specified in the interface implementation code</param>
        /// <remarks>Any existing MemberFormatter can't be used for generation of fingerprint because none of them generate equal signatures
        /// for an interface member and its implementation in the following cases:
        /// 1. Explicitly implemented members
        /// 2. Different names of generic arguments in interface and in implementing type
        /// 3. Implementation of interface with generic type parameters
        /// The last point is especially interesting because it makes GetFingerprint method context-sensitive:
        /// it returns signatures of interface members depending on generic parameters of the implementing type (GenericInstanceType parameter).</remarks>
        public static string GetFingerprint(MemberReference memberReference, GenericInstanceType genericInterface = null)
        {
            // An interface contains only the signatures of methods, properties, events or indexers. 

            StringBuilder buf = new StringBuilder();
            
            var unifiedTypeNames = new Dictionary<string, string>();
            FillUnifiedMemberTypeNames(unifiedTypeNames, memberReference as IGenericParameterProvider);// Fill the member generic parameters unified names as M0, M1....
            FillUnifiedTypeNames(unifiedTypeNames, memberReference.DeclaringType, genericInterface);// Fill the type generic parameters unified names as T0, T1....

            if (memberReference is IMethodSignature)
            {
                IMethodSignature methodSignature = (IMethodSignature)memberReference;
                buf.Append(GetUnifiedTypeName(methodSignature.ReturnType, unifiedTypeNames)).Append(" ");
                buf.Append(SimplifyName(memberReference.Name)).Append(" ");
                AppendParameters(buf, methodSignature.Parameters, unifiedTypeNames);
            }
            if (memberReference is PropertyDefinition)
            {
                PropertyDefinition propertyReference = (PropertyDefinition)memberReference;
                buf.Append(GetUnifiedTypeName(propertyReference.PropertyType, unifiedTypeNames)).Append(" ");
                if (propertyReference.GetMethod != null)
                    buf.Append("get").Append(" ");
                if (propertyReference.SetMethod != null)
                    buf.Append("set").Append(" ");
                buf.Append(SimplifyName(memberReference.Name)).Append(" ");
                AppendParameters(buf, propertyReference.Parameters, unifiedTypeNames);
            }
            if (memberReference is EventDefinition)
            {
                EventDefinition eventReference = (EventDefinition)memberReference;
                buf.Append(GetUnifiedTypeName(eventReference.EventType, unifiedTypeNames)).Append(" ");
                buf.Append(SimplifyName(memberReference.Name)).Append(" ");
            }
            
            var memberUnifiedTypeNames = new Dictionary<string, string>();
            FillUnifiedMemberTypeNames(memberUnifiedTypeNames, memberReference as IGenericParameterProvider);
            if (memberUnifiedTypeNames.Any())// Add generic arguments to the fingerprint. SomeMethod<T>() != SomeMethod()
            {
                buf.Append(" ").Append(string.Join(" ", memberUnifiedTypeNames.Values));
            }
            return buf.ToString();
        }

        /// <summary>
        /// If it's the name of explicitly implemented member return only short name
        /// </summary>
        private static string SimplifyName(string memberName)
        {
            int nameBorder = memberName.LastIndexOf(".", StringComparison.InvariantCulture);
            if (nameBorder > 0)
            {
                return memberName.Substring(nameBorder + 1, memberName.Length - nameBorder - 1);
            }
            return memberName;
        }

        /// <summary>
        /// Resolve generic type names of the type type based on interface implementation details
        /// </summary>
        private static void FillUnifiedTypeNames(Dictionary<string, string> unifiedTypeNames,
            IGenericParameterProvider type, 
            GenericInstanceType genericInterface)
        {
            if (type == null)
                return;

            int i = 0;
            foreach (var genericParameter in type.GenericParameters)
            {
                if (unifiedTypeNames.ContainsKey(genericParameter.Name))
                    continue;
                
                // if generic type can be resolved with interface implementation parameters
                if (genericInterface != null
                    && i < genericInterface.GenericArguments.Count)
                {
                    unifiedTypeNames[genericParameter.Name] = genericInterface.GenericArguments[i].FullName;
                }
                else
                {
                    unifiedTypeNames[genericParameter.Name] = genericParameter.Name; 
                }
                ++i;
            }
        }

        /// <summary>
        /// Resolve generic type names of the member based on the order of generic arguments
        /// </summary>
        private static void FillUnifiedMemberTypeNames(Dictionary<string, string> unifiedTypeNames,
            IGenericParameterProvider member)
        {
            if (member == null)
                return;

            int i = 0;
            foreach (var genericParameter in member.GenericParameters)
            {
                if (unifiedTypeNames.ContainsKey(genericParameter.Name))
                    continue;
                unifiedTypeNames[genericParameter.Name] = "MemberGenericType" + i;

                ++i;
            }
        }

        private static void AppendParameters(StringBuilder buf, Collection<ParameterDefinition> parameters, Dictionary<string, string> genericTypes)
        {
            buf.Append("(");
            foreach (var parameterDefinition in parameters)
            {
                buf.Append(GetUnifiedTypeName(parameterDefinition.ParameterType, genericTypes)).Append(", ");
            }
            buf.Append(")");
        }

        private static string GetUnifiedTypeName(TypeReference type, Dictionary<string, string> genericTypes)
        {
            var genericInstance = type as IGenericInstance;
            if (genericInstance != null && genericInstance.HasGenericArguments)
            {
                return
                    $"{type.Namespace}.{type.Name}<{string.Join(",", genericInstance.GenericArguments.Select(i => GetUnifiedTypeName(i, genericTypes)))}>";
            }

            return genericTypes.ContainsKey(type.Name)
                ? genericTypes[type.Name]
                : SpecialTypesChk(type, genericTypes);
        }

        private static string SpecialTypesChk(TypeReference type, Dictionary<string, string> genericTypes)
        {
            if (type.IsArray)
            {
                var elements = type.GetElementType();
                if (elements != null && elements.IsGenericParameter && genericTypes.ContainsKey(elements.Name))
                    return type.FullName.Replace(elements.Name, genericTypes[elements.Name]);
                else
                {
                    return type.FullName;
                }
            }
            else
                return type.FullName;
        }

        public static string GetExplicitTypeName(MemberReference memberReference)
        {
            var overrides = GetOverrides(memberReference);
            if (overrides == null)
            {
                throw new ArgumentException("Unsupported explicitly implemented member");
            }

            var declaringType = overrides.First().DeclaringType;
            return declaringType.GetElementType().FullName;
        }

        public static bool IsExplicitlyImplemented(MemberReference memberReference)
        {
            var overrides = GetOverrides(memberReference);
            return overrides?.Count > 0;
        }

        /// <summary>
        /// Get which members are overriden by memberReference
        /// </summary>
        public static Collection<MethodReference> GetOverrides(MemberReference memberReference)
        {
            if (memberReference is MethodDefinition)
            {
                MethodDefinition methodDefinition = (MethodDefinition)memberReference;
                return methodDefinition.Overrides;
            }
            if (memberReference is PropertyDefinition)
            {
                PropertyDefinition propertyDefinition = (PropertyDefinition)memberReference;
                return (propertyDefinition.GetMethod ?? propertyDefinition.SetMethod)?.Overrides;
            }
            if (memberReference is EventDefinition)
            {
                EventDefinition evendDefinition = (EventDefinition)memberReference;
                return evendDefinition.AddMethod.Overrides;
            }

            return null;
        }

        public static bool IsDestructor(MethodDefinition method)
        {
            return method.IsFamily 
                && method.Name == "Finalize" 
                && method.Overrides.Count == 1 
                && method.Overrides[0].DeclaringType.FullName == "System.Object";
        }

        public static bool IsOperator(MethodReference method)
        {
            return method.Name.StartsWith("op_", StringComparison.Ordinal);
        }

        public static bool DocIdCheck(XmlNode a, XmlElement b)
        {
            if (b.LocalName != "Member" || a.LocalName != "Member")
                return false;

            var oldMembersDocid = b.SelectSingleNode("MemberSignature[@Language='DocId']/@Value")?.Value;
            var seenNembersDocid = a.SelectSingleNode("MemberSignature[@Language='DocId']/@Value")?.Value;

            if (oldMembersDocid != null && seenNembersDocid != null)
            {
                if (!seenNembersDocid.Equals(oldMembersDocid))
                    return true;
            }

            return false;
        }

        // For some types, the generic parameters resolved by mono declared by their declaring type
        // This method will return the generic parameters declared by type itself
        public static List<GenericParameter> GetGenericParameters(TypeDefinition type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var genericParameters = new List<GenericParameter>(type.GenericParameters);
            List<TypeReference> declTypes = GetDeclaringTypes(type);
            int maxGenArgs = GetGenericArgumentCount(type);
            
            for (int i = 0; i < declTypes.Count - 1; ++i)
            {
                int remove = System.Math.Min(maxGenArgs,
                        GetGenericArgumentCount(declTypes[i]));
                maxGenArgs -= remove;
                while (remove-- > 0)
                    genericParameters.RemoveAt(0);
            }
            return genericParameters;
        }

        public static List<System.Xml.XmlNode> RemoveInvalidAssemblyInfo(XmlElement Nodeinfo, bool No_assembly_versions, String Type)
        {
            List<System.Xml.XmlNode> assemblyDelList = new List<System.Xml.XmlNode>();
            if (No_assembly_versions)
                return assemblyDelList;

            var filter = Type == "Member" ? "AssemblyInfo" : $"/{Type}/AssemblyInfo";

            var assemblyFromXml = Nodeinfo
                                     .SelectNodes(filter)
                                     .Cast<XmlElement>();

            foreach (var item in assemblyFromXml)
            {
                if (item.GetElementsByTagName("AssemblyVersion").Count == 0)
                    assemblyDelList.Add(item);
            }
            return assemblyDelList;
        }

        public static bool CheckRemoveByImporter(DocsNodeInfo info, string keyName, List<DocumentationImporter> DocImports, IEnumerable<DocumentationImporter> SetImports)
        {
            foreach (DocumentationImporter i in DocImports)
            {
                if (i.CheckRemoveByMapping(info, keyName))
                    return true;
            }

            if (SetImports != null)
            {
                foreach (var i in SetImports)
                {
                    if (i.CheckRemoveByMapping(info, keyName))
                        return true;
                }
            }

            return false;
        }

        public static bool IsEiiIgnoredMethod(MethodReference method, MethodReference imethod)
        {
            if (DocUtils.IsExplicitlyImplemented(method.Resolve()) && !method.Resolve().IsSpecialName)
                if (IsIgnored(imethod))
                    return true;

            return false;
        }

        public static TypeDefinition FixUnnamedParameters(TypeDefinition type)
        {
            foreach (var method in type.Methods)
            {
                var unnamedParameterIndex = 1;
                foreach (var item in method.Parameters)
                {
                    if (string.IsNullOrEmpty(item.Name))
                    {
                        item.Name = $"unnamedParam{unnamedParameterIndex++}";
                    }
                }
            }

            return type;
        }
    }
}
