using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Documentation.Updater.Formatters;
using Mono.Documentation.Util;

namespace Mono.Documentation.Updater
{
    public class VBFullMemberFormatter : MemberFormatter
    {
        public override string Language => Consts.VbNet;

        public override string SingleLineComment => "'";

        public VBFullMemberFormatter() : this(null) {}
        public VBFullMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendNamespace(StringBuilder buf, TypeReference type)
        {
            string ns = DocUtils.GetNamespace(type);
            if (GetVBType(type.FullName) == null && !string.IsNullOrEmpty(ns) && ns != "System")
                buf.Append(ns).Append('.');
            return buf;
        }

        protected virtual string GetVBType(string t)
        {
            // make sure there are no modifiers in the type string (add them back before returning)
            string typeToCompare = t;
            string[] splitType = null;
            if (t.Contains(' '))
            {
                splitType = t.Split(' ');
                typeToCompare = splitType[0];
            }

            switch (typeToCompare)
            {
                case "System.Byte": typeToCompare = "Byte"; break;
                case "System.SByte": typeToCompare = "SByte"; break;
                case "System.Int16": typeToCompare = "Short"; break;
                case "System.Int32": typeToCompare = "Integer"; break;
                case "System.Int64": typeToCompare = "Long"; break;

                case "System.UInt16": typeToCompare = "UShort"; break;
                case "System.UInt32": typeToCompare = "UInteger"; break;
                case "System.UInt64": typeToCompare = "ULong"; break;

                case "System.Single": typeToCompare = "Single"; break;
                case "System.Double": typeToCompare = "Double"; break;
                case "System.Decimal": typeToCompare = "Decimal"; break;
                case "System.Boolean": typeToCompare = "Boolean"; break;
                case "System.Char": typeToCompare = "Char"; break;
                case "System.String": typeToCompare = "String"; break;
                case "System.Object": typeToCompare = "Object"; break;
            }

            if (splitType != null)
            {
                // re-add modreq/modopt if it was there
                splitType[0] = typeToCompare;
                typeToCompare = string.Join(" ", splitType);
            }
            return typeToCompare == t ? null : typeToCompare;
        }

        protected override StringBuilder AppendTypeName(StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            if (type is GenericParameter)
                return AppendGenericParameterConstraints(buf, (GenericParameter)type, context).Append(type.Name);
            string t = type.FullName;
            if (!t.StartsWith("System."))
            {
                return base.AppendTypeName(buf, type, context);
            }

            string s = GetVBType(t);
            if (s != null)
            {
                context.NextDynamicFlag();
                return buf.Append(s);
            }

            return base.AppendTypeName(buf, type, context);
        }

        private StringBuilder AppendGenericParameterConstraints(StringBuilder buf, GenericParameter type, IAttributeParserContext context)
        {
            if (MemberFormatterState != MemberFormatterState.WithinGenericTypeParameters)
                return buf;
            GenericParameterAttributes attrs = type.Attributes;
            bool isout = (attrs & GenericParameterAttributes.Covariant) != 0;
            bool isin = (attrs & GenericParameterAttributes.Contravariant) != 0;
            if (isin)
                buf.Append("In ");
            else if (isout)
                buf.Append("Out ");
            return buf;
        }

        protected override string GetTypeDeclaration(TypeDefinition type)
        {
            string visibility = GetTypeVisibility(type.Attributes);
            if (visibility == null)
                return null;

            StringBuilder buf = new StringBuilder();

            buf.Append(visibility);
            buf.Append(" ");

            MemberFormatter full = new VBMemberFormatter(this.TypeMap);
            if (DocUtils.IsDelegate(type))
            {
                buf.Append("Delegate ");
                MethodDefinition invoke = type.GetMethod("Invoke");
                bool isFunction = invoke.ReturnType.FullName != "System.Void";
                if (isFunction)
                    buf.Append("Function ");
                else
                    buf.Append("Sub ");
                buf.Append(GetName(type));
                AppendParameters(buf, invoke, invoke.Parameters);
                if (isFunction)
                {
                    buf.Append(" As ");
                    buf.Append(full.GetName(invoke.ReturnType, AttributeParserContext.Create(invoke.MethodReturnType))).Append(" ");
                }

                return buf.ToString();
            }

            if (type.IsAbstract && !type.IsInterface && !IsModule(type))
                buf.Append("MustInherit ");
            if (type.IsSealed && !DocUtils.IsDelegate(type) && !type.IsValueType && !IsModule(type))
                buf.Append("NotInheritable ");
            buf.Replace(" MustInherit NotInheritable", "");

            buf.Append(GetTypeKind(type));
            buf.Append(" ");
            buf.Append(GetVBType(type.FullName) == null
                    ? GetName(type)
                    : type.Name);

            if (!type.IsEnum)
            {
                TypeReference basetype = type.BaseType;
                if (basetype != null && basetype.FullName == "System.Object" || type.IsValueType)
                    basetype = null;

                if (basetype != null)
                {
                    buf.Append(GetLineEnding()).Append("Inherits ");
                    buf.Append(full.GetName(basetype));
                }

                List<string> interfaceNames = DocUtils.GetUserImplementedInterfaces(type)
                        .Select(iface => full.GetName(iface))
                        .OrderBy(s => s)
                        .ToList();
                if (interfaceNames.Count > 0)
                {
                    buf.Append(GetLineEnding()).Append("Implements ");
                    buf.Append(string.Join(", ", interfaceNames));
                }
            }

            return buf.ToString();
        }

        protected override string[] GenericTypeContainer
        {
            get { return new[] {"(Of ", ")"}; }
        }
        
        static string GetTypeKind(TypeDefinition t)
        {
            if (IsModule(t))
                return "Module";
            if (t.IsEnum)
                return "Enum";
            if (t.IsValueType)
                return "Structure";
            if (t.IsClass || t.FullName == "System.Enum")
                return "Class";
            if (t.IsInterface)
                return "Interface";
            throw new ArgumentException(t.FullName);
        }

        static string GetTypeVisibility(TypeAttributes ta)
        {
            switch (ta & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.Public:
                case TypeAttributes.NestedPublic:
                    return "Public";

                case TypeAttributes.NestedFamily:
                    return "Protected";
                case TypeAttributes.NestedFamORAssem:
                    return "Protected Friend";

                default:
                    return null;
            }
        }

        protected override StringBuilder AppendGenericType(StringBuilder buf, TypeReference type, IAttributeParserContext context, bool appendGeneric = true, bool useTypeProjection = false, bool isTypeofOperator = false)
        {
            List<TypeReference> decls = DocUtils.GetDeclaringTypes(
                    type is GenericInstanceType ? type.GetElementType() : type);
            List<TypeReference> genArgs = GetGenericArguments(type);
            int argIdx = 0;
            int displayedInParentArguments = 0;
            bool insertNested = false;
            foreach (var decl in decls)
            {
                TypeReference declDef = decl.Resolve() ?? decl;
                if (insertNested)
                {
                    buf.Append(NestedTypeSeparator);
                }
                insertNested = true;

                AppendTypeName(buf, declDef, context);

                int argumentCount = DocUtils.GetGenericArgumentCount(declDef);
                int notYetDisplayedArguments = argumentCount - displayedInParentArguments;
                displayedInParentArguments = argumentCount;// nested TypeReferences have parents' generic arguments, but we shouldn't display them
                if (notYetDisplayedArguments > 0)
                {
                    buf.Append(GenericTypeContainer[0]);
                    var origState = MemberFormatterState;
                    MemberFormatterState = MemberFormatterState.WithinGenericTypeParameters;
                    for (int i = 0; i < notYetDisplayedArguments; ++i)
                    {
                        if (i > 0)
                            buf.Append(", ");
                        var genArg = genArgs[argIdx++];
                        _AppendTypeName(buf, genArg, context, useTypeProjection: useTypeProjection);
                        var genericParameter = genArg as GenericParameter;
                        if (genericParameter != null)
                            AppendConstraints(buf, genericParameter);
                    }
                    MemberFormatterState = origState;
                    buf.Append(GenericTypeContainer[1]);
                }
            }
            return buf;
        }

        protected override StringBuilder AppendGenericTypeConstraints(StringBuilder buf, TypeReference type)
        {
            return buf;
        }

        private void AppendConstraints(StringBuilder buf, GenericParameter genArg)
        {
            if (MemberFormatterState == MemberFormatterState.WithinGenericTypeParameters)
            {
                // check to avoid such a code: 
                // Public Class MyList(Of A As {Class, Generic.IList(Of B --->As {Class, A}<----), New}, B As {Class, A})
                return;
            }

            GenericParameterAttributes attrs = genArg.Attributes;
#if NEW_CECIL
            Mono.Collections.Generic.Collection<GenericParameterConstraint> constraints = genArg.Constraints;
#else
            IList<TypeReference> constraints = genArg.Constraints;
#endif
            if (attrs == GenericParameterAttributes.NonVariant && constraints.Count == 0)
                return;

            bool isref = (attrs & GenericParameterAttributes.ReferenceTypeConstraint) != 0;
            bool isvt = (attrs & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
            bool isnew = (attrs & GenericParameterAttributes.DefaultConstructorConstraint) != 0;
            bool comma = false;

            if (!isref && !isvt && !isnew && constraints.Count == 0)
                return;
            int constraintsCount = Convert.ToInt32(isref) + Convert.ToInt32(isvt) + Convert.ToInt32(isnew) + constraints.Count;
            buf.Append(" As ");

            if (constraintsCount > 1 && !isvt)
                buf.Append("{");
            if (isref)
            {
                buf.Append("Class");
                comma = true;
            }
            else if (isvt)
            {
                buf.Append("Structure");
                comma = true;
            }
            if (constraints.Count > 0 && !isvt)
            {
                if (comma)
                    buf.Append(", ");

#if NEW_CECIL
                buf.Append(GetTypeName(constraints[0].ConstraintType));
                for (int i = 1; i < constraints.Count; ++i)
                    buf.Append(", ").Append(GetTypeName(constraints[i].ConstraintType));
#else
                buf.Append(GetTypeName(constraints[0]));
                for (int i = 1; i < constraints.Count; ++i)
                    buf.Append(", ").Append(GetTypeName(constraints[i]));
#endif
            }
            if (isnew && !isvt)
            {
                if (comma)
                    buf.Append(", ");
                buf.Append("New");
            }
            if (constraintsCount > 1 && !isvt)
                buf.Append("}");
        }

        protected override string GetConstructorDeclaration(MethodDefinition constructor)
        {
            StringBuilder buf = new StringBuilder();
            AppendVisibility(buf, constructor);
            if (buf.Length == 0 && !constructor.IsStatic) //Static constructor is needed
                return null;

            if (constructor.IsStatic)
                buf.Append(buf.Length == 0 ? "Shared" : " Shared");

            buf.Append(" Sub New ");
            AppendParameters(buf, constructor, constructor.Parameters);

            return buf.ToString();
        }

        protected override string GetMethodDeclaration(MethodDefinition method)
        {
            if (method.HasCustomAttributes && method.CustomAttributes.Cast<CustomAttribute>().Any(
                        ca => ca.GetDeclaringType() == "System.Diagnostics.Contracts.ContractInvariantMethodAttribute"))
                return null;

            
            // Special signature for destructors.
            if (method.Name == "Finalize" && method.Parameters.Count == 0)
                return GetFinalizerName(method);

            StringBuilder buf = new StringBuilder();
            if (DocUtils.IsExtensionMethod(method))
                buf.Append("<Extension()>" + GetLineEnding());

            AppendVisibility(buf, method);
            if (buf.Length == 0 &&
                    !(DocUtils.IsExplicitlyImplemented(method) && !method.IsSpecialName))
                return null;

            AppendModifiers(buf, method);
            if (buf.Length != 0)
                buf.Append(" ");
            bool isFunction = method.MethodReturnType.ReturnType.FullName != "System.Void";
            if (!DocUtils.IsOperator(method))
            {
                if (isFunction)
                    buf.Append("Function ");
                else
                    buf.Append("Sub ");
            }
            AppendMethodName(buf, method);

            AppendGenericMethod(buf, method).Append(" ");
            AppendParameters(buf, method, method.Parameters);
            AppendGenericMethodConstraints(buf, method);
            if (isFunction)
                buf.Append(" As ").Append(GetTypeName(method.ReturnType, AttributeParserContext.Create(method.MethodReturnType)));

            if (DocUtils.IsExplicitlyImplemented(method))
            {
                TypeReference iface;
                MethodReference ifaceMethod;
                DocUtils.GetInfoForExplicitlyImplementedMethod(method, out iface, out ifaceMethod);
                buf.Append(" Implements ")
                    .Append(new VBMemberFormatter(this.TypeMap).GetName(iface))
                    .Append('.')
                    .Append(ifaceMethod.Name);
            }

            return buf.ToString();
        }

        protected override StringBuilder AppendMethodName(StringBuilder buf, MethodDefinition method)
        {
            if (DocUtils.IsExplicitlyImplemented(method))
            {
                return buf.Append(method.Name.Split('.').Last());
            }

            if (DocUtils.IsOperator(method))
            {
                // this is an operator
                switch (method.Name)
                {
                    case "op_Implicit":
                    case "op_Explicit":
                        buf.Length--; // remove the last space, which assumes a member name is coming
                        return buf;
                    case "op_Addition":
                    case "op_UnaryPlus":
                        return buf.Append("Operator +");
                    case "op_Subtraction":
                    case "op_UnaryNegation":
                        return buf.Append("Operator -");
                    case "op_IntegerDivision":
                        return buf.Append("Operator \\");
                    case "op_Division":
                        return buf.Append("Operator /");
                    case "op_Multiply":
                        return buf.Append("Operator *");
                    case "op_Modulus":
                        return buf.Append("Operator Mod");
                    case "op_BitwiseAnd":
                        return buf.Append("Operator And");
                    case "op_BitwiseOr":
                        return buf.Append("Operator Or");
                    case "op_ExclusiveOr":
                        return buf.Append("Operator Xor");
                    case "op_LeftShift":
                        return buf.Append("Operator <<");
                    case "op_RightShift":
                        return buf.Append("Operator >>");
                    case "op_LogicalNot":
                        return buf.Append("Operator Not");
                    case "op_OnesComplement":
                        return buf.Append("Operator Not");
                    case "op_True":
                        return buf.Append("Operator IsTrue");
                    case "op_False":
                        return buf.Append("Operator IsFalse");
                    case "op_Equality":
                        return buf.Append("Operator ==");
                    case "op_Inequality":
                        return buf.Append("Operator !=");
                    case "op_LessThan":
                        return buf.Append("Operator <");
                    case "op_LessThanOrEqual":
                        return buf.Append("Operator <=");
                    case "op_GreaterThan":
                        return buf.Append("Operator >");
                    case "op_GreaterThanOrEqual":
                        return buf.Append("Operator >=");
                    case "op_Like":
                        return buf.Append("Operator Like");
                    default:
                        return base.AppendMethodName(buf, method);
                }
            }
            else
                return base.AppendMethodName(buf, method);
        }

        protected override StringBuilder AppendGenericMethodConstraints(StringBuilder buf, MethodDefinition method)
        {
            return buf;
        }

        protected override string RefTypeModifier
        {
            get { return ""; }
        }

        protected override string GetFinalizerName(MethodDefinition method)
        {
            return method.Name + " ()";
        }

        protected override StringBuilder AppendVisibility(StringBuilder buf, MethodDefinition method)
        {
            if (method == null)
                return buf;
            if (method.IsPublic)
                return buf.Append("Public");
            if (method.IsFamily)
                return buf.Append("Protected");
            if (method.IsFamilyOrAssembly)
                return buf.Append("Protected Friend");
            return buf;
        }

        protected override StringBuilder AppendModifiers(StringBuilder buf, MethodDefinition method)
        {
            string modifiers = String.Empty;
            if (method.IsStatic && !IsModule(method.DeclaringType)) modifiers += " Shared";
            if (IsIteratorMethod(method)) modifiers += " Iterator";
            if (method.IsVirtual && !method.IsAbstract)
            {
                if ((method.Attributes & MethodAttributes.NewSlot) != 0) modifiers += " Overridable";
                else modifiers += " Overrides";
            }
            TypeDefinition declType = (TypeDefinition)method.DeclaringType;
            if (method.IsAbstract && !declType.IsInterface) modifiers += " MustOverride";
            if (method.IsFinal) modifiers += " NotOverridable";
            if (modifiers == " MustOverride NotOverridable") modifiers = "";
            if (modifiers == " Overridable NotOverridable") modifiers = "";

            switch (method.Name)
            {
                case "op_Implicit":
                    modifiers += " Widening Operator CType";
                    break;
                case "op_Explicit":
                    modifiers += " Narrowing Operator CType";
                    break;
            }

            return buf.Append(modifiers);
        }

        protected override StringBuilder AppendGenericMethod(StringBuilder buf, MethodDefinition method)
        {
            if (method.IsGenericMethod())
            {
                IList<GenericParameter> args = method.GenericParameters;
                if (args.Count > 0)
                {
                    buf.Append(GenericTypeContainer[0]);
                    buf.Append(args[0].Name);
                    AppendConstraints(buf, args[0]);
                    for (int i = 1; i < args.Count; ++i)
                    {
                        buf.Append(", ").Append(args[i].Name);
                        AppendConstraints(buf, args[0]);
                    }
                    buf.Append(GenericTypeContainer[1]);
                }
            }
            return buf;
        }

        protected override StringBuilder AppendParameters(StringBuilder buf, MethodDefinition method, IList<ParameterDefinition> parameters)
        {
            return AppendParameters(buf, method, parameters, '(', ')');
        }

        private StringBuilder AppendParameters(StringBuilder buf, MethodDefinition method, IList<ParameterDefinition> parameters, char begin, char end)
        {
            buf.Append(begin);

            if (parameters.Count > 0)
            {
                AppendParameter(buf, parameters[0]);
                for (int i = 1; i < parameters.Count; ++i)
                {
                    buf.Append(", ");
                    AppendParameter(buf, parameters[i]);
                }
            }

            return buf.Append(end);
        }

        private StringBuilder AppendParameter(StringBuilder buf, ParameterDefinition parameter)
        {
            if (parameter.IsOptional)
            {
                buf.Append("Optional ");
            }
            if (parameter.ParameterType is ByReferenceType)
            {
                buf.Append("ByRef ");
            }
            if (parameter.HasCustomAttributes)
            {
                var isParams = parameter.CustomAttributes.Any(ca => ca.AttributeType.Name == "ParamArrayAttribute");
                if (isParams)
                    buf.AppendFormat("ParamArray ");
            }
            buf.Append(parameter.Name);
            buf.Append(" As ");
            buf.Append(GetTypeName(parameter.ParameterType, AttributeParserContext.Create(parameter)));
            if (parameter.HasDefault && parameter.IsOptional && parameter.HasConstant)
            {
                var parameterValue = new AttributeFormatter().MakeAttributesValueString(parameter.Constant, parameter.ParameterType);
                buf.AppendFormat(" = {0}", parameterValue == "null" ? "Nothing" : parameterValue);
            }
            return buf;
        }

        protected override string GetPropertyDeclaration(PropertyDefinition property)
        {
            string getVisible = null;
            if (DocUtils.IsAvailablePropertyMethod(property.GetMethod))
                getVisible = AppendVisibility(new StringBuilder(), property.GetMethod).ToString();
            string setVisible = null;
            if (DocUtils.IsAvailablePropertyMethod(property.SetMethod))
                setVisible = AppendVisibility(new StringBuilder(), property.SetMethod).ToString();

            if (setVisible == null && getVisible == null)
                return null;

            StringBuilder buf = new StringBuilder();
            IEnumerable<MemberReference> defs = property.DeclaringType.GetDefaultMembers();
            bool indexer = false;
            foreach (MemberReference mi in defs)
            {
                if (mi == property)
                {
                    indexer = true;
                    break;
                }
            }
            if (indexer)
                buf.Append("Default ");
            if (getVisible != null && (setVisible == null || (setVisible != null && getVisible == setVisible)))
                buf.Append(getVisible);
            else if (setVisible != null && getVisible == null)
                buf.Append(setVisible);
            else
                buf.Append("Public");

            // Pick an accessor to use for static/virtual/override/etc. checks.
            var method = property.SetMethod;
            if (method == null)
                method = property.GetMethod;

            string modifiers = String.Empty;
            if (method.IsStatic && !IsModule(method.DeclaringType)) modifiers += " Shared";
            if (method.IsVirtual && !method.IsAbstract)
            {
                if ((method.Attributes & MethodAttributes.NewSlot) != 0)
                    modifiers += " Overridable";
                else
                    modifiers += " Overrides";
            }
            TypeDefinition declDef = (TypeDefinition)method.DeclaringType;
            if (method.IsAbstract && !declDef.IsInterface)
                modifiers += " MustOverride";
            if (method.IsFinal)
                modifiers += " NotOverridable";
            if (modifiers == " MustOverride NotOverridable")
                modifiers = "";
            if (modifiers == " Overridable NotOverridable")
                modifiers = "";
            buf.Append(modifiers).Append(' ');

            if (getVisible != null && setVisible == null)
                buf.Append("ReadOnly ");

            buf.Append("Property ");
            buf.Append(property.Name.Split('.').Last());

            if (property.Parameters.Count != 0)
            {
                AppendParameters(buf, method, property.Parameters, '(', ')');
            }
            buf.Append(" As ");
            buf.Append(GetTypeName(property.PropertyType, AttributeParserContext.Create(property)));
            if (DocUtils.IsExplicitlyImplemented(property.GetMethod))
            {
                TypeReference iface;
                MethodReference ifaceMethod;
                DocUtils.GetInfoForExplicitlyImplementedMethod(method, out iface, out ifaceMethod);
                buf.Append(" Implements ")
                    .Append(new VBMemberFormatter(this.TypeMap).GetName(iface))
                    .Append('.')
                    .Append(DocUtils.GetPropertyName(property, NestedTypeSeparator).Split('.').Last());
            }
            return buf.ToString();
        }

        protected override string GetFieldDeclaration(FieldDefinition field)
        {
            TypeDefinition declType = (TypeDefinition)field.DeclaringType;
            if (declType.IsEnum && field.Name == "value__")
                return null; // This member of enums aren't documented.

            StringBuilder buf = new StringBuilder();
            AppendFieldVisibility(buf, field);
            if (buf.Length == 0)
                return null;

            if (declType.IsEnum)
                return field.Name;

            if (field.IsStatic && !field.IsLiteral && !IsModule(field.DeclaringType))
                buf.Append(" Shared");
            if (field.IsInitOnly)
                buf.Append(" ReadOnly");
            if (field.IsLiteral)
                buf.Append(" Const");

            buf.Append(' ').Append(field.Name);
            buf.Append(" As ").Append(GetTypeName(field.FieldType, AttributeParserContext.Create(field))).Append(' ');
            DocUtils.AppendFieldValue(buf, field);

            return buf.ToString();
        }

        static void AppendFieldVisibility(StringBuilder buf, FieldDefinition field)
        {
            if (field.IsPublic)
            {
                buf.Append("Public");
                return;
            }
            if (field.IsFamily)
            {
                buf.Append("Protected");
            }
            if (field.IsFamilyOrAssembly)
            {
                buf.Append("Protected Friend");
            }
        }

        protected override string GetEventDeclaration(EventDefinition e)
        {
            StringBuilder buf = new StringBuilder();
            bool isPublicEII = IsPublicEII(e);

            if (AppendVisibility(buf, e.AddMethod).Length == 0 && !isPublicEII)
            {
                return null;
            }
            if (e.DeclaringType.IsInterface) // There is no access modifiers in interfaces
            {
                buf.Clear();
            }
            AppendModifiers(buf, e.AddMethod);
            if (e.AddMethod.CustomAttributes.All(
                i => i.AttributeType.FullName != Consts.CompilerGeneratedAttribute)
                && !e.DeclaringType.IsInterface)// There is no 'Custom' modifier in interfaces
            {
                if (buf.Length > 0)
                    buf.Append(' ');
                buf.Append("Custom");
            }

            if (buf.Length > 0)
                buf.Append(' ');
            buf.Append("Event ");
            if (isPublicEII)
                buf.Append(e.Name.Split('.').Last());
            else
                buf.Append(e.Name);
            buf.Append(" As ").Append(GetTypeName(e.EventType, AttributeParserContext.Create(e.AddMethod.Parameters[0]))).Append(' ');
            if (isPublicEII) {
                var dotIndex = e.Name.LastIndexOf ('.');
                dotIndex = dotIndex > -1 ? dotIndex : e.Name.Length;
                buf.Append($"Implements {e.Name.Substring(0, dotIndex)}");
            }

            return buf.ToString();
        }

        protected override char[] ArrayDelimeters
        {
            get { return new[] {'(', ')'}; }
        }

        public override bool IsSupported(TypeReference tref)
        {
            return !tref.Name.Contains('*');
        }

        public override bool IsSupported(MemberReference mref)
        {
            var field = mref as FieldDefinition;
            if (field != null)
            {
                return IsSupported(field.FieldType)
                    && IsSupportedNaming(field);
            }

            var method = mref as MethodDefinition;
            if (method != null)
            {
                return IsSupported(method.ReturnType)
                    && method.Parameters.All(i => IsSupported(i.ParameterType))
                    && IsSupportedNaming(method);
            }

            var property = mref as PropertyDefinition;
            if (property != null)
            {
                return IsSupported(property.PropertyType)
                    && IsSupportedNaming(property);
            }

            var @event = mref as EventDefinition;
            if (@event != null)
            {
                return IsSupported(@event.EventType)
                    && IsSupportedNaming(@event);
            }

            var @event2 = mref as AttachedEventReference;
            if (@event2 != null)
                return true;

            var prop = mref as AttachedPropertyReference;
            if (prop != null)
                return true;

            throw new NotSupportedException("Unsupported member type: " + mref.GetType().FullName);
        }

        private bool IsSupportedNaming(EventDefinition @event)
        {
            return !@event.Name.Equals(@event.EventType.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsSupportedNaming(PropertyDefinition property)
        {
            return !property.Name.Equals(property.DeclaringType.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsSupportedNaming(MethodDefinition method)
        {
            var allTypes = method.Parameters.Select(i => i.ParameterType)
                                            .Concat(method.GenericParameters)
                                            .Concat(new[] { method.ReturnType })
                                            .ToList();
            foreach (var typeReference in allTypes)
            {
                foreach (var typeReference2 in allTypes)
                {
                    // if there are types which differ only in letter case
                    if (typeReference.Name.Equals(typeReference2.Name, StringComparison.InvariantCultureIgnoreCase)
                        && typeReference.Name != typeReference2.Name)
                        return false;
                }
            }

            foreach (var parameterDefinition in method.Parameters)
            {
                foreach (var parameterDefinition2 in method.Parameters)
                {
                    // if there're parameters which names are case-insensitively equal
                    if (parameterDefinition.Name.Equals(parameterDefinition2.Name, StringComparison.InvariantCultureIgnoreCase)
                        && parameterDefinition.Name != parameterDefinition2.Name)
                        return false;
                }
            }
            return true;
        }

        private bool IsSupportedNaming(FieldDefinition field)
        {
            return !field.Name.Equals(field.DeclaringType.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsModule(TypeDefinition typeDefinition)
        {
            return typeDefinition.CustomAttributes.Any(i => i.AttributeType.FullName == "Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute")
                || typeDefinition.Methods.Any(DocUtils.IsExtensionMethod);
        }

        private bool IsIteratorMethod(MethodDefinition method)
        {
            return method.CustomAttributes.Any(i => i.AttributeType.FullName == "System.Runtime.CompilerServices.IteratorStateMachineAttribute");
        }
    }
}
