using Mono.Cecil;
using Mono.Documentation.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mono.Documentation.Updater
{
    public abstract class MemberFormatter
    {
        public TypeMap TypeMap { get; set; }

        public MemberFormatter(TypeMap typemap) => TypeMap = typemap;

        public virtual string Language
        {
            get { return ""; }
        }

        public virtual string SingleLineComment
        {
            get { return "//"; }
        }

        public string GetName (MemberReference member, bool appendGeneric = true, bool useTypeProjection = true, bool isTypeofOperator = false)
        {
            return GetName (member, EmptyAttributeParserContext.Empty(), appendGeneric, useTypeProjection, isTypeofOperator);
        }

        public virtual string GetName (MemberReference member, IAttributeParserContext context, bool appendGeneric = true, bool useTypeProjection = true, bool isTypeofOperator = false)
        {
            TypeReference type = member as TypeReference;
            if (type != null)
                return GetTypeName (type, context, appendGeneric, useTypeProjection, isTypeofOperator);
            MethodReference method = member as MethodReference;
            if (method != null && method.Name == ".ctor") // method.IsConstructor
                return GetConstructorName (method);
            if (method != null)
                return GetMethodName (method);
            PropertyReference prop = member as PropertyReference;
            if (prop != null)
                return GetPropertyName (prop);
            FieldReference field = member as FieldReference;
            if (field != null)
                return GetFieldName (field);
            EventReference e = member as EventReference;
            if (e != null)
                return GetEventName (e);
            throw new NotSupportedException ("Can't handle: " +
                        (member == null ? "null" : member.GetType ().ToString ()));
        }

        protected virtual string GetTypeName (TypeReference type, bool appendGeneric = true, bool useTypeProjection = true)
        {
            return GetTypeName (type, EmptyAttributeParserContext.Empty(), appendGeneric, useTypeProjection: useTypeProjection);
        }

        protected virtual string GetTypeName (TypeReference type, IAttributeParserContext context, bool appendGeneric = true, bool useTypeProjection = true, bool isTypeofOperator = false)
        {
            if (type == null)
                throw new ArgumentNullException (nameof (type));

            var typeName = _AppendTypeName (new StringBuilder (type.Name.Length), type, context, appendGeneric, useTypeProjection, isTypeofOperator).ToString ();

            return RemoveMod (typeName);
        }

        public static string RemoveMod (string typeName)
        {
            if (string.IsNullOrWhiteSpace (typeName)) return typeName;

            // For custom modifiers (modopt/modreq), we are just excising them
            // via string manipulation for simplicity; since these cannot be
            // expressed in C#. If finer control is needed in the future, you can
            // use IModifierType, PointerType, ByReferenceType, etc.

            int modIndex = System.Math.Max (typeName.LastIndexOf ("modopt(", StringComparison.Ordinal), typeName.LastIndexOf ("modreq(", StringComparison.Ordinal));
            if (modIndex > 0)
            {
                var tname = typeName.Substring (0, modIndex - 1);
                var parenIndex = typeName.LastIndexOf (')');
                if (parenIndex == typeName.Length - 2)
                { // see if there's metadata like a pointer
                    tname += typeName.Last ();
                }
                typeName = tname;
            }

            modIndex = System.Math.Max (typeName.LastIndexOf ("modopt(", StringComparison.Ordinal), typeName.LastIndexOf ("modreq(", StringComparison.Ordinal));
            if (modIndex >= 0)
                return RemoveMod (typeName);
            else
                return typeName;
        }

        protected virtual char[] ArrayDelimeters
        {
            get { return new char[] { '[', ']' }; }
        }

        protected virtual MemberFormatterState MemberFormatterState { get; set; }

        protected virtual StringBuilder AppendRequiredModifierTypeName(StringBuilder buf, RequiredModifierType type, IAttributeParserContext context)
        {
            return _AppendTypeName (buf, type.ElementType, context);
        }

        protected virtual StringBuilder AppendOptionalModifierTypeName(StringBuilder buf, OptionalModifierType type, IAttributeParserContext context)
        {
            return _AppendTypeName(buf, type.ElementType, context);
        }

        protected virtual StringBuilder AppendArrayTypeName(StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            var elementType = type.GetElementType ();
            if (type is TypeSpecification typeSpecification)
            {
                elementType = typeSpecification.ElementType;
            }

            var isNullableType = false;
            if (!elementType.IsValueType)
            {
                isNullableType = context.IsNullable ();
            }

            _AppendTypeName (buf, elementType, context);
            AppendNullableSymbol (buf, elementType, isNullableType);

            return AppendArrayModifiers (buf, (ArrayType)type);
        }

        protected virtual bool ShouldStripModFromTypeName
        {
            get => true;
        }

        protected StringBuilder _AppendTypeName (StringBuilder buf, TypeReference type, IAttributeParserContext context, bool appendGeneric = true, bool useTypeProjection = false, bool isTypeofOperator = false)
        {
            if (type == null)
                return buf;

            StringBuilder interimBuilder = new StringBuilder();

            if (ShouldStripModFromTypeName && type is RequiredModifierType)
            {
                AppendRequiredModifierTypeName(interimBuilder, type as RequiredModifierType, context);
                return SetBuffer(buf, interimBuilder, useTypeProjection: useTypeProjection);
            }
            if (ShouldStripModFromTypeName && type is OptionalModifierType)
            {
                AppendOptionalModifierTypeName(interimBuilder, type as OptionalModifierType, context);
                return SetBuffer(buf, interimBuilder, useTypeProjection: useTypeProjection);
            }
            if (type is ArrayType)
            {
                AppendArrayTypeName(interimBuilder, type, context);
                return SetBuffer(buf, interimBuilder, useTypeProjection: useTypeProjection);
            }
            if (type is ByReferenceType)
            {
                AppendRefTypeName (interimBuilder, type, context);
                return SetBuffer(buf, interimBuilder, useTypeProjection: useTypeProjection);
            }
            if (type is PointerType)
            {
                AppendPointerTypeName (interimBuilder, type, context);
                return SetBuffer(buf, interimBuilder, useTypeProjection: useTypeProjection);
            }
            if (type is GenericParameter)
            {
                AppendTypeName (interimBuilder, type, context);
                return SetBuffer(buf, interimBuilder, useTypeProjection: useTypeProjection);
            }

            AppendNamespace (interimBuilder, type);

            if (type.IsRequiredModifier)
            {
                try
                {
                    var rtype = type.Resolve ();
                    if (rtype != null)
                        type = rtype;
                }
                catch (Exception)
                {
                    // Suppress resolving error for UWP libraries.
                    // It seems, they never have `type.IsRequiredModifier == true`, but just in case.
                }
            }

            GenericInstanceType genInst = type as GenericInstanceType;
            if (IsSpecialGenericNullableValueType (genInst))   // For special C# nullable value type only.
            {
                AppendSpecialGenericNullableValueTypeName (buf, genInst, context);
                return SetBuffer (buf, interimBuilder, useTypeProjection: useTypeProjection);
            }

            if (type.GenericParameters.Count == 0 &&
                    (genInst == null ? true : genInst.GenericArguments.Count == 0))
            {
                AppendFullTypeName (interimBuilder, type, context);
                return SetBuffer(buf, interimBuilder, useTypeProjection: useTypeProjection);
            }
            AppendGenericType (interimBuilder, type, context, appendGeneric, useTypeProjection, isTypeofOperator);
            return SetBuffer(buf, interimBuilder, useTypeProjection: useTypeProjection);
        }

        protected virtual bool IsSpecialGenericNullableValueType(GenericInstanceType genInst)
        {
            // For special C# nullable value type only, the CSharpFullMemberFormatter subclass will override the method.
            return false;
        }

        protected virtual StringBuilder AppendSpecialGenericNullableValueTypeName(StringBuilder buf, GenericInstanceType genInst, IAttributeParserContext context, bool appendGeneric = true, bool useTypeProjection = true)
        {
            // For special C# nullable value type only, the CSharpFullMemberFormatter subclass will override the method.
            return buf;
        }

        private StringBuilder SetBuffer(StringBuilder buf, StringBuilder interimBuilder, bool useTypeProjection)
        {
            var interimResult = interimBuilder.ToString();

            var projectedname = interimResult;

            if (useTypeProjection && TypeMap != null)
            {
                var leftGenContainer = this.GenericTypeContainer.FirstOrDefault();
                var leftGenIndex = interimResult.IndexOf(leftGenContainer);
                string genericPiece = "";
                if (leftGenIndex > 0)
                {
                    // chop off the generic portion for comparison purposes
                    genericPiece = interimResult.Substring(leftGenIndex, interimResult.Length - leftGenIndex);
                    interimResult = interimResult.Substring(0, leftGenIndex);
                }

                var mapLang = string.IsNullOrWhiteSpace(this.Language) ? "C#" : this.Language;
                projectedname = TypeMap?.GetTypeName(mapLang, interimResult) + genericPiece ?? projectedname;
            }

            buf.Append(projectedname);

            return buf;
        }

        protected virtual StringBuilder AppendNamespace (StringBuilder buf, TypeReference type)
        {
            string ns = DocUtils.GetNamespace (type);
            if (ns != null && ns.Length > 0)
                buf.Append (ns).Append ('.');
            return buf;
        }

        protected virtual StringBuilder AppendFullTypeName (StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            if (type.DeclaringType != null)
                AppendFullTypeName (buf, type.DeclaringType, context).Append (NestedTypeSeparator);
            return AppendTypeName (buf, type, context);
        }

        protected virtual StringBuilder AppendTypeName (StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            context.NextDynamicFlag();
            return AppendTypeName (buf, type.Name);
        }

        protected virtual StringBuilder AppendTypeName (StringBuilder buf, string typename)
        {
            int n = typename.IndexOf ("`");
            if (n >= 0)
                return buf.Append (typename.Substring (0, n));
            return buf.Append (typename);
        }

        protected virtual StringBuilder AppendArrayModifiers (StringBuilder buf, ArrayType array)
        {
            buf.Append (ArrayDelimeters[0]);
            int rank = array.Rank;
            if (rank > 1)
                buf.Append (new string (',', rank - 1));
            return buf.Append (ArrayDelimeters[1]);
        }

        protected virtual string RefTypeModifier
        {
            get { return "@"; }
        }

        protected virtual StringBuilder AppendRefTypeName (StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            TypeSpecification spec = type as TypeSpecification;
            return _AppendTypeName(buf, spec != null ? spec.ElementType : type.GetElementType(), context);
        }

        protected virtual string PointerModifier
        {
            get { return "*"; }
        }

        protected virtual StringBuilder AppendPointerTypeName (StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            TypeSpecification spec = type as TypeSpecification;
            return _AppendTypeName (buf, spec != null ? spec.ElementType : type.GetElementType (), context)
                    .Append (PointerModifier);
        }

        protected virtual string[] GenericTypeContainer
        {
            get { return new string[] { "<", ">" }; }
        }

        protected virtual string NestedTypeSeparator
        {
            get { return "."; }
        }

        protected virtual StringBuilder AppendGenericType (StringBuilder buf, TypeReference type, IAttributeParserContext context, bool appendGeneric = true, bool useTypeProjection = true, bool isTypeofOperator = false)
        {
            int argIdx = 0;
            int prev = 0;
            bool insertNested = false;
            var declaringType = type is GenericInstanceType ? type.GetElementType () : type;
            List<TypeReference> decls = DocUtils.GetDeclaringTypes (declaringType);
            List<TypeReference> genArgs = GetGenericArguments (type);
            foreach (var decl in decls)
            {
                if (insertNested)
                {
                    buf.Append (NestedTypeSeparator);
                }

                insertNested = true;
                TypeReference declDef = decl.Resolve () ?? decl;
                AppendTypeName (buf, declDef, context);
                int ac = DocUtils.GetGenericArgumentCount (declDef);
                int c = ac - prev;
                prev = ac;
                if (appendGeneric && c > 0)
                {
                    buf.Append (GenericTypeContainer[0]);

                    var currentTypeArguments = genArgs.Skip(argIdx).Take(c);
                    if (IsTypeOfOperatorWithUnboundGenericType(decl, currentTypeArguments, isTypeofOperator))
                    {
                        buf.Append(string.Join(string.Empty, Enumerable.Repeat(",", currentTypeArguments.Count() - 1)));
                    }
                    else
                    {
                        var origState = MemberFormatterState;
                        MemberFormatterState = MemberFormatterState.WithinGenericTypeParameters;
                        AppendGenericTypeParameter (buf, genArgs[argIdx++], context, useTypeProjection);
                        for (int i = 1; i < c; ++i)
                        {
                            buf.Append (",");
                            AppendGenericTypeParameter (buf, genArgs[argIdx++], context, useTypeProjection);
                        }

                        MemberFormatterState = origState;
                    }

                    buf.Append (GenericTypeContainer[1]);
                }
            }

            return buf;
        }

        private bool IsTypeOfOperatorWithUnboundGenericType(TypeReference type, IEnumerable<TypeReference> genericArguments, bool isTypeofOperator)
        {
            if (isTypeofOperator)
            {
                var argumentCount = genericArguments.Count() > 0 ? genericArguments.Count().ToString() : string.Empty;
                var genericTypeSeparator = $"`{argumentCount}";
                var genericTypeSeparatorIndex = type.FullName.LastIndexOf(genericTypeSeparator);

                var leftAngleBracketIndex = type.FullName.IndexOf("<", genericTypeSeparatorIndex);
                var validLeftAngleBracketIndex = genericTypeSeparatorIndex + genericTypeSeparator.Length + 1;

                // If the type is an unbound generic type that type hasn't angle brackets after its full name.
                var notExistParameterType = genericTypeSeparatorIndex >= 0 && leftAngleBracketIndex != validLeftAngleBracketIndex;
                var notPassParameterType = genericArguments.All(i => i is GenericParameter);

                return notExistParameterType && notPassParameterType;
            }

            return false;
        }

        private void AppendGenericTypeParameter (StringBuilder buf, TypeReference type, IAttributeParserContext context, bool useTypeProjection = true)
        {
            var isNullableType = false;
            if (type is GenericInstanceType)
            {
                isNullableType = context.IsNullable();
                buf.Append(GetTypeName(type, context));
            }
            else
            {
                if (!type.IsValueType)
                {
                    isNullableType = context.IsNullable();
                }

                _AppendTypeName(buf, type, context, useTypeProjection: useTypeProjection);
            }

            AppendNullableSymbol(buf, type, isNullableType);
        }

        private StringBuilder AppendNullableSymbol (StringBuilder buf, TypeReference type, bool? isNullableType)
        {
            return buf.Append(GetTypeNullableSymbol(type, isNullableType));
        }

        protected virtual string GetTypeNullableSymbol(TypeReference type, bool? isNullableType)
        {
            return string.Empty;
        }

        protected List<TypeReference> GetGenericArguments (TypeReference type)
        {
            var args = new List<TypeReference> ();
            GenericInstanceType inst = type as GenericInstanceType;
            if (inst != null)
                args.AddRange (inst.GenericArguments);
            else
                args.AddRange (type.GenericParameters);
            return args;
        }

        protected virtual StringBuilder AppendGenericTypeConstraints (StringBuilder buf, TypeReference type)
        {
            return buf;
        }

        protected virtual string GetConstructorName (MethodReference constructor)
        {
            return constructor.Name;
        }

        protected virtual string GetMethodName (MethodReference method)
        {
            return method.Name;
        }

        protected virtual string GetPropertyName (PropertyReference property)
        {
            return property.Name;
        }

        protected virtual string GetFieldName (FieldReference field)
        {
            return field.Name;
        }

        protected virtual string GetEventName (EventReference e)
        {
            return e.Name;
        }

        public virtual string GetDeclaration (TypeReference tref)
        {
            if (!IsSupported(tref))
                return null;
            var typeSpec = tref as TypeSpecification;
            if (typeSpec != null && typeSpec.Resolve () == null && typeSpec.IsArray && typeSpec.ContainsGenericParameter)
            {
                //HACK: there's really no good reference for a generic parameter array, so we're going to use object
                return "T:System.Array";
            }
            TypeDefinition def = tref.Resolve ();
            if (def != null)
                return GetTypeDeclaration (def);
            else
                return GetTypeName (tref);
        }

        public virtual string GetDeclaration (MemberReference mreference)
        {
            if (!IsSupported(mreference))
               return null; 
            return GetDeclaration (mreference.Resolve ());
        }

        string GetDeclaration (IMemberDefinition member)
        {
            if (member == null)
                throw new ArgumentNullException ("member");
            TypeDefinition type = member as TypeDefinition;
            if (type != null)
                return GetTypeDeclaration (type);
            MethodDefinition method = member as MethodDefinition;
            if (method != null && method.IsConstructor)
                return GetConstructorDeclaration (method);
            if (method != null)
                return GetMethodDeclaration (method);
            PropertyDefinition prop = member as PropertyDefinition;
            if (prop != null)
                return GetPropertyDeclaration (prop);
            FieldDefinition field = member as FieldDefinition;
            if (field != null)
                return GetFieldDeclaration (field);
            EventDefinition e = member as EventDefinition;
            if (e != null)
                return GetEventDeclaration (e);
            AttachedEventDefinition ae = member as AttachedEventDefinition;
            if (ae != null)
                return GetAttachedEventDeclaration (ae);
            AttachedPropertyDefinition ap = member as AttachedPropertyDefinition;
            if (ap != null)
                return GetAttachedPropertyDeclaration (ap);
            throw new NotSupportedException ("Can't handle: " + member.GetType ().ToString ());
        }

        protected virtual string GetTypeDeclaration (TypeDefinition type)
        {
            if (type == null)
                throw new ArgumentNullException ("type");
            StringBuilder buf = new StringBuilder (type.Name.Length);
            _AppendTypeName (buf, type, null);
            AppendGenericTypeConstraints (buf, type);
            return buf.ToString ();
        }

        protected virtual string GetConstructorDeclaration (MethodDefinition constructor)
        {
            return GetConstructorName (constructor);
        }

        protected virtual string GetMethodDeclaration (MethodDefinition method)
        {
            if (method.HasCustomAttributes && method.CustomAttributes.Cast<CustomAttribute> ().Any (
                        ca => ca.GetDeclaringType () == "System.Diagnostics.Contracts.ContractInvariantMethodAttribute"))
                return null;

            // Special signature for destructors.
            if (method.Name == "Finalize" && method.Parameters.Count == 0)
                return GetFinalizerName (method);

            StringBuilder buf = new StringBuilder ();

            AppendVisibility (buf, method);
            if (buf.Length == 0 &&
                    !(DocUtils.IsExplicitlyImplemented (method) && !method.IsSpecialName))
                return null;

            AppendModifiers (buf, method);

            if (buf.Length != 0)
                buf.Append (" ");

            AppendReturnTypeName (buf, method);
            AppendMethodName (buf, method);
            AppendGenericMethod (buf, method).Append (" ");
            AppendParameters (buf, method, method.Parameters);
            AppendGenericMethodConstraints (buf, method);
            return buf.ToString ();
        }


        private StringBuilder AppendReturnTypeName (StringBuilder buf, MethodDefinition method)
        {
            var context = AttributeParserContext.Create (method.MethodReturnType);
            var isNullableType = context.IsNullable ();
            var returnTypeName = GetTypeName (method.ReturnType, context);
            buf.Append (returnTypeName);
            buf.Append (GetTypeNullableSymbol (method.ReturnType, isNullableType));
            buf.Append (" ");

            return buf;
        }

        protected virtual StringBuilder AppendMethodName (StringBuilder buf, MethodDefinition method)
        {
            return buf.Append (method.Name);
        }

        protected virtual string GetFinalizerName (MethodDefinition method)
        {
            return "Finalize";
        }

        protected virtual StringBuilder AppendVisibility (StringBuilder buf, MethodDefinition method)
        {
            return buf;
        }

        protected virtual StringBuilder AppendModifiers (StringBuilder buf, MethodDefinition method)
        {
            return buf;
        }

        protected virtual StringBuilder AppendGenericMethod (StringBuilder buf, MethodDefinition method)
        {
            return buf;
        }

        protected virtual StringBuilder AppendParameters (StringBuilder buf, MethodDefinition method, IList<ParameterDefinition> parameters)
        {
            return buf;
        }

        protected virtual StringBuilder AppendGenericMethodConstraints (StringBuilder buf, MethodDefinition method)
        {
            return buf;
        }

        protected virtual string GetPropertyDeclaration (PropertyDefinition property)
        {
            return GetPropertyName (property);
        }

        protected virtual string GetFieldDeclaration (FieldDefinition field)
        {
            return GetFieldName (field);
        }

        protected virtual string GetEventDeclaration (EventDefinition e)
        {
            return GetEventName (e);
        }

        protected virtual string GetAttachedEventDeclaration(Mono.Documentation.Util.AttachedEventDefinition e)
        {
            return $"see Add{e.Name}Handler, and Remove{e.Name}Handler";
        }

        protected virtual string GetAttachedPropertyDeclaration(AttachedPropertyDefinition a)
        {
            // check get and set member and craft string according
            string getter = $"Get{a.Name}";
            string setter = $"Set{a.Name}";

            var get = a.GetMethod;
            var set = a.SetMethod;

            if (get != null && set == null)
                return $"see {getter}";
            else if (set != null && get == null)
                return $"see {setter}";
            else
                return $"see {getter}, and {setter}";
        }

        public virtual bool IsSupported(TypeReference tref) => true;

        public virtual bool IsSupported(MemberReference mref) => true;
        

        protected static bool IsPublicEII (EventDefinition e)
        {
            bool isPublicEII = false;
            if (e.AddMethod.HasOverrides)
            {
                var resolvedAddMethod = e.AddMethod.Overrides[0].Resolve ();
                var resolvedInterface = e.AddMethod.Overrides[0].DeclaringType.Resolve ();
                if (DocUtils.IsPublic (resolvedInterface) && resolvedAddMethod != null && resolvedAddMethod.IsPublic)
                {
                    isPublicEII = true;
                }
            }

            return isPublicEII;
        }

        public virtual MemberFormatter UsageFormatter { get; protected set; }

        public static string GetLineEnding()
        {
            return "\n";
        }

        protected string CamelCase(string name)
        {
            return Char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }
}