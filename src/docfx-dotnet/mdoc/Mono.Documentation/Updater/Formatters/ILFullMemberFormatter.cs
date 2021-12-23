using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Mono.Cecil;

using Mono.Documentation.Util;

namespace Mono.Documentation.Updater.Formatters
{
    public class ILFullMemberFormatter : MemberFormatter
    {

        public override string Language
        {
            get { return "ILAsm"; }
        }

        protected override string NestedTypeSeparator
        {
            get
            {
                return "/";
            }
        }

        public ILFullMemberFormatter() : this(null) {}
        public ILFullMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendNamespace (StringBuilder buf, TypeReference type)
        {
            if (GetBuiltinType (type.FullName) != null)
                return buf;
            string ns = DocUtils.GetNamespace (type);
            if (ns != null && ns.Length > 0)
            {
                if (type.IsValueType)
                    buf.Append ("valuetype ");
                else
                    buf.Append ("class ");
                buf.Append (ns).Append ('.');
            }
            return buf;
        }

        protected static string GetBuiltinType (string t)
        {
            switch (t)
            {
                case "System.Byte": return "unsigned int8";
                case "System.SByte": return "int8";
                case "System.Int16": return "int16";
                case "System.Int32": return "int32";
                case "System.Int64": return "int64";
                case "System.IntPtr": return "native int";

                case "System.UInt16": return "unsigned int16";
                case "System.UInt32": return "unsigned int32";
                case "System.UInt64": return "unsigned int64";
                case "System.UIntPtr": return "native unsigned int";

                case "System.Single": return "float32";
                case "System.Double": return "float64";
                case "System.Boolean": return "bool";
                case "System.Char": return "char";
                case "System.Void": return "void";
                case "System.String": return "string";
                case "System.Object": return "object";
            }
            return null;
        }

        protected override StringBuilder AppendTypeName (StringBuilder buf, string typename)
        {
            return buf.Append (typename);
        }

        protected override StringBuilder AppendTypeName (StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            if (type is GenericParameter)
            {
                AppendGenericParameterConstraints (buf, (GenericParameter)type).Append (type.Name);
                return buf;
            }

            string s = GetBuiltinType (type.FullName);
            if (s != null)
            {
                return buf.Append (s);
            }
            return base.AppendTypeName (buf, type, context);
        }

        private StringBuilder AppendGenericParameterConstraints (StringBuilder buf, GenericParameter type)
        {
            if (MemberFormatterState != MemberFormatterState.WithinGenericTypeParameters)
            {
                return buf.Append (type.Owner is TypeReference ? "!" : "!!");
            }
            GenericParameterAttributes attrs = type.Attributes;
            if ((attrs & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                buf.Append ("class ");
            if ((attrs & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                buf.Append ("struct ");
            if ((attrs & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
                buf.Append (".ctor ");
            MemberFormatterState = 0;

#if NEW_CECIL
            Mono.Collections.Generic.Collection<GenericParameterConstraint> constraints = type.Constraints;
            if (constraints.Count > 0)
            {
                var full = new ILFullMemberFormatter ();
                buf.Append ("(").Append (full.GetName (constraints[0].ConstraintType));
                for (int i = 1; i < constraints.Count; ++i)
                {
                    buf.Append (", ").Append (full.GetName (constraints[i].ConstraintType));
                }
                buf.Append (") ");
            }
#else
            IList<TypeReference> constraints = type.Constraints;
            if (constraints.Count > 0)
            {
                var full = new ILFullMemberFormatter (this.TypeMap);
                buf.Append ("(").Append (full.GetName (constraints[0]));
                for (int i = 1; i < constraints.Count; ++i)
                {
                    buf.Append (", ").Append (full.GetName (constraints[i]));
                }
                buf.Append (") ");
            }
#endif
            MemberFormatterState = MemberFormatterState.WithinGenericTypeParameters;

            if ((attrs & GenericParameterAttributes.Covariant) != 0)
                buf.Append ("+ ");
            if ((attrs & GenericParameterAttributes.Contravariant) != 0)
                buf.Append ("- ");
            return buf;
        }

        protected override string GetTypeDeclaration (TypeDefinition type)
        {
            string visibility = GetTypeVisibility (type.Attributes);
            if (visibility == null)
                return null;

            StringBuilder buf = new StringBuilder ();

            buf.Append (".class ");
            if (type.IsNested)
                buf.Append ("nested ");
            buf.Append (visibility).Append (" ");
            if (type.IsInterface)
                buf.Append ("interface ");
            if (type.IsSequentialLayout)
                buf.Append ("sequential ");
            if (type.IsAutoLayout)
                buf.Append ("auto ");
            if (type.IsAnsiClass)
                buf.Append ("ansi ");
            if (type.IsAbstract)
                buf.Append ("abstract ");
            if (type.IsSerializable)
                buf.Append ("serializable ");
            if (type.IsSealed)
                buf.Append ("sealed ");
            if (type.IsBeforeFieldInit)
                buf.Append ("beforefieldinit ");
            var state = MemberFormatterState;
            MemberFormatterState = MemberFormatterState.WithinGenericTypeParameters;
            buf.Append (GetName (type));
            MemberFormatterState = state;
            var full = new ILFullMemberFormatter (this.TypeMap);
            if (type.BaseType != null)
            {
                buf.Append (" extends ");
                if (type.BaseType.FullName == "System.Object")
                    buf.Append ("System.Object");
                else
                    buf.Append (full.GetName (type.BaseType).Substring ("class ".Length));
            }
            bool first = true;
            foreach (var name in type.Interfaces.Where (i => DocUtils.IsPublic (i.InterfaceType.Resolve ()))
                    .Select (i => full.GetName (i.InterfaceType))
                    .OrderBy (n => n))
            {
                if (first)
                {
                    buf.Append (" implements ");
                    first = false;
                }
                else
                {
                    buf.Append (", ");
                }
                buf.Append (name);
            }

            return buf.ToString ();
        }

        protected override StringBuilder AppendGenericType (StringBuilder buf, TypeReference type, IAttributeParserContext context, bool appendGeneric = true, bool useTypeProjection = false, bool isTypeofOperator = false)
        {
            List<TypeReference> decls = DocUtils.GetDeclaringTypes (
                    type is GenericInstanceType ? type.GetElementType () : type);
            bool first = true;
            foreach (var decl in decls)
            {
                TypeReference declDef = decl.Resolve () ?? decl;
                if (!first)
                {
                    buf.Append (NestedTypeSeparator);
                }
                first = false;
                AppendTypeName (buf, declDef, context);
            }
            buf.Append ('<');
            first = true;
            foreach (TypeReference arg in GetGenericArguments (type))
            {
                if (!first)
                    buf.Append (", ");
                first = false;
                _AppendTypeName (buf, arg, context);
            }
            buf.Append ('>');
            return buf;
        }

        static string GetTypeVisibility (TypeAttributes ta)
        {
            switch (ta & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.Public:
                case TypeAttributes.NestedPublic:
                    return "public";

                case TypeAttributes.NestedFamily:
                case TypeAttributes.NestedFamORAssem:
                    return "protected";

                default:
                    return null;
            }
        }

        protected override string GetConstructorDeclaration (MethodDefinition constructor)
        {
            return GetMethodDeclaration (constructor);
        }

        protected override string GetMethodDeclaration (MethodDefinition method)
        {
            if (method.IsPrivate && !DocUtils.IsExplicitlyImplemented (method))
                return null;

            var buf = new StringBuilder ();
            buf.Append (".method ");
            AppendVisibility (buf, method);
            if (method.IsStatic)
                buf.Append ("static ");
            if (method.IsHideBySig)
                buf.Append ("hidebysig ");
            if (method.IsPInvokeImpl && method.PInvokeInfo != null)
            {
                var info = method.PInvokeInfo;

                buf.Append ("pinvokeimpl (\"")
                    .Append (info.Module.Name)
                    .Append ("\" as \"")
                    .Append (info.EntryPoint)
                    .Append ("\"");
                
                if (info.IsCharSetAuto)
                    buf.Append (" auto");
                if (info.IsCharSetUnicode)
                    buf.Append (" unicode");
                if (info.IsCharSetAnsi)
                    buf.Append (" ansi");
                if (info.IsCallConvCdecl)
                    buf.Append (" cdecl");
                if (info.IsCallConvStdCall)
                    buf.Append (" stdcall");
                if (info.IsCallConvWinapi)
                    buf.Append (" winapi");
                if (info.IsCallConvThiscall)
                    buf.Append (" thiscall");
                if (info.SupportsLastError)
                    buf.Append (" lasterr");
                buf.Append (")");
            }
            if (method.IsSpecialName)
                buf.Append ("specialname ");
            if (method.IsRuntimeSpecialName)
                buf.Append ("rtspecialname ");
            if (method.IsNewSlot)
                buf.Append ("newslot ");
            if (method.IsVirtual)
                buf.Append ("virtual ");
            if (!method.IsStatic)
                buf.Append ("instance ");
            _AppendTypeName (buf, method.ReturnType, AttributeParserContext.Create (method.MethodReturnType));
            buf.Append (' ')
                .Append (method.Name);
            if (method.IsGenericMethod ())
            {
                var state = MemberFormatterState;
                MemberFormatterState = MemberFormatterState.WithinGenericTypeParameters;
                IList<GenericParameter> args = method.GenericParameters;
                if (args.Count > 0)
                {
                    buf.Append ("<");
                    _AppendTypeName (buf, args[0], null);
                    for (int i = 1; i < args.Count; ++i)
                        _AppendTypeName (buf.Append (", "), args[i], null);
                    buf.Append (">");
                }
                MemberFormatterState = state;
            }

            buf.Append ('(');
            bool first = true;
            for (int i = 0; i < method.Parameters.Count; ++i)
            {
                var param = method.Parameters[i];
                if (!first)
                    buf.Append (", ");
                first = false;

                if (param.IsOut) buf.Append ("[out] ");
                else if (param.IsIn) buf.Append ("[in]");

                _AppendTypeName (buf, param.ParameterType, AttributeParserContext.Create (param));
                if (param.ParameterType.IsByReference) buf.Append ("&");
                buf.Append (' ');
                buf.Append (param.Name);
            }
            buf.Append (')');
            if (method.IsIL)
                buf.Append (" cil");
            if (method.IsRuntime)
                buf.Append (" runtime");
            if (method.IsManaged)
                buf.Append (" managed");

            return buf.ToString ();
        }

        protected override StringBuilder AppendMethodName (StringBuilder buf, MethodDefinition method)
        {
            if (DocUtils.IsExplicitlyImplemented (method))
            {
                TypeReference iface;
                MethodReference ifaceMethod;
                DocUtils.GetInfoForExplicitlyImplementedMethod (method, out iface, out ifaceMethod);
                return buf.Append (new CSharpMemberFormatter (this.TypeMap).GetName (iface))
                    .Append ('.')
                    .Append (ifaceMethod.Name);
            }
            return base.AppendMethodName (buf, method);
        }

        protected override string RefTypeModifier
        {
            get { return ""; }
        }

        protected override StringBuilder AppendVisibility (StringBuilder buf, MethodDefinition method)
        {
            if (method.IsPublic)
                return buf.Append ("public ");
            if (method.IsFamilyAndAssembly)
                return buf.Append ("familyandassembly");
            if (method.IsFamilyOrAssembly)
                return buf.Append ("familyorassembly");
            if (method.IsFamily)
                return buf.Append ("family");
            return buf;
        }

        protected override StringBuilder AppendModifiers (StringBuilder buf, MethodDefinition method)
        {
            string modifiers = String.Empty;
            if (method.IsStatic) modifiers += " static";
            if (method.IsVirtual && !method.IsAbstract)
            {
                if ((method.Attributes & MethodAttributes.NewSlot) != 0) modifiers += " virtual";
                else modifiers += " override";
            }
            TypeDefinition declType = (TypeDefinition)method.DeclaringType;
            if (method.IsAbstract && !declType.IsInterface) modifiers += " abstract";
            if (method.IsFinal) modifiers += " sealed";
            if (modifiers == " virtual sealed") modifiers = "";

            return buf.Append (modifiers);
        }

        protected override StringBuilder AppendGenericMethod (StringBuilder buf, MethodDefinition method)
        {
            if (method.IsGenericMethod ())
            {
                IList<GenericParameter> args = method.GenericParameters;
                if (args.Count > 0)
                {
                    buf.Append ("<");
                    buf.Append (args[0].Name);
                    for (int i = 1; i < args.Count; ++i)
                        buf.Append (",").Append (args[i].Name);
                    buf.Append (">");
                }
            }
            return buf;
        }

        protected override StringBuilder AppendParameters (StringBuilder buf, MethodDefinition method, IList<ParameterDefinition> parameters)
        {
            return AppendParameters (buf, method, parameters, '(', ')');
        }

        private StringBuilder AppendParameters (StringBuilder buf, MethodDefinition method, IList<ParameterDefinition> parameters, char begin, char end)
        {
            buf.Append (begin);

            if (parameters.Count > 0)
            {
                if (DocUtils.IsExtensionMethod (method))
                    buf.Append ("this ");
                AppendParameter (buf, parameters[0]);
                for (int i = 1; i < parameters.Count; ++i)
                {
                    buf.Append (", ");
                    AppendParameter (buf, parameters[i]);
                }
            }

            return buf.Append (end);
        }

        private StringBuilder AppendParameter (StringBuilder buf, ParameterDefinition parameter)
        {
            if (parameter.ParameterType is ByReferenceType)
            {
                if (parameter.IsOut)
                    buf.Append ("out ");
                else
                    buf.Append ("ref ");
            }
            buf.Append (GetName (parameter.ParameterType)).Append (" ");
            return buf.Append (parameter.Name);
        }

        protected override string GetPropertyDeclaration (PropertyDefinition property)
        {
            MethodDefinition gm = null, sm = null;

            string get_visible = null;
            if ((gm = property.GetMethod) != null &&
                    (DocUtils.IsExplicitlyImplemented (gm) ||
                     (!gm.IsPrivate && !gm.IsAssembly && !gm.IsFamilyAndAssembly)))
                get_visible = AppendVisibility (new StringBuilder (), gm).ToString ();
            string set_visible = null;
            if ((sm = property.SetMethod) != null &&
                    (DocUtils.IsExplicitlyImplemented (sm) ||
                     (!sm.IsPrivate && !sm.IsAssembly && !sm.IsFamilyAndAssembly)))
                set_visible = AppendVisibility (new StringBuilder (), sm).ToString ();

            if ((set_visible == null) && (get_visible == null))
                return null;

            StringBuilder buf = new StringBuilder ()
                .Append (".property ");
            if (!(gm ?? sm).IsStatic)
                buf.Append ("instance ");
            _AppendTypeName (buf, property.PropertyType, AttributeParserContext.Create (property));
            buf.Append (' ').Append (property.Name);
            if (!property.HasParameters || property.Parameters.Count == 0)
                return buf.ToString ();

            buf.Append ('(');
            bool first = true;
            foreach (ParameterDefinition p in property.Parameters)
            {
                if (!first)
                    buf.Append (", ");
                first = false;
                _AppendTypeName (buf, p.ParameterType, AttributeParserContext.Create (p));
            }
            buf.Append (')');

            return buf.ToString ();
        }

        protected override string GetFieldDeclaration (FieldDefinition field)
        {
            TypeDefinition declType = (TypeDefinition)field.DeclaringType;
            if (declType.IsEnum && field.Name == "value__")
                return null; // This member of enums aren't documented.

            StringBuilder buf = new StringBuilder ();
            AppendFieldVisibility (buf, field);
            if (buf.Length == 0)
                return null;

            buf.Insert (0, ".field ");

            if (field.IsStatic)
                buf.Append ("static ");
            if (field.IsInitOnly)
                buf.Append ("initonly ");
            if (field.IsLiteral)
                buf.Append ("literal ");
            _AppendTypeName (buf, field.FieldType, AttributeParserContext.Create (field));
            buf.Append (' ').Append (field.Name);
            AppendFieldValue (buf, field);

            return buf.ToString ();
        }

        static StringBuilder AppendFieldVisibility (StringBuilder buf, FieldDefinition field)
        {
            if (field.IsPublic)
                return buf.Append ("public ");
            if (field.IsFamilyAndAssembly)
                return buf.Append ("familyandassembly ");
            if (field.IsFamilyOrAssembly)
                return buf.Append ("familyorassembly ");
            if (field.IsFamily)
                return buf.Append ("family ");
            return buf;
        }

        static StringBuilder AppendFieldValue (StringBuilder buf, FieldDefinition field)
        {
            // enums have a value__ field, which we ignore
            if (field.DeclaringType.IsGenericType ())
                return buf;
            if (field.HasConstant && field.IsLiteral)
            {
                object val = null;
                try
                {
                    val = field.Constant;
                }
                catch
                {
                    return buf;
                }
                if (val == null)
                    buf.Append (" = ").Append ("null");
                else if (val is Enum)
                    buf.Append (" = ")
                        .Append (GetBuiltinType (field.DeclaringType.GetUnderlyingType ().FullName))
                        .Append ('(')
                        .Append (val.ToString ())
                        .Append (')');
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
                    buf.Append (" = ");
                    if (val is string)
                        buf.Append ("\"" + value + "\"");
                    else
                        buf.Append (GetBuiltinType (field.DeclaringType.GetUnderlyingType ().FullName))
                            .Append ('(')
                            .Append (value)
                            .Append (')');
                }
            }
            return buf;
        }

        protected override string GetEventDeclaration (EventDefinition e)
        {
            StringBuilder buf = new StringBuilder ();
            if (AppendVisibility (buf, e.AddMethod).Length == 0 && !IsPublicEII (e))
            {
                return null;
            }

            buf.Length = 0;
            buf.Append (".event ")
                .Append (GetName (e.EventType))
                .Append (' ')
                .Append (e.Name);

            return buf.ToString ();
        }
    }
}