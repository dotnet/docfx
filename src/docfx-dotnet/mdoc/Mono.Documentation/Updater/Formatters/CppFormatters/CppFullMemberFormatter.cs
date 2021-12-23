using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Collections.Generic;
using Mono.Documentation.Util;

namespace Mono.Documentation.Updater.Formatters.CppFormatters
{
    public class CppFullMemberFormatter : MemberFormatter
    {
        protected virtual bool AppendHatOnReturn => true;

        public override string Language => Consts.CppCli;

        protected override string RefTypeModifier => " %";

        protected virtual string HatModifier => " ^";

        protected override string NestedTypeSeparator => "::";

        protected override bool ShouldStripModFromTypeName => false;

        public CppFullMemberFormatter() : this(null) {}
        public CppFullMemberFormatter(TypeMap map) : base(map) { }

        protected virtual IEnumerable<string> NoHatTypes => new List<string>()
        {
            "System.Void",
            "Mono.Cecil.GenericParameter"
        };

        protected override StringBuilder AppendNamespace (StringBuilder buf, TypeReference type)
        {
            string ns = DocUtils.GetNamespace(type, NestedTypeSeparator);
            if (GetCppType(type.FullName) == null && !string.IsNullOrEmpty(ns) && ns != "System")
                buf.Append(ns).Append(NestedTypeSeparator);
            return buf;
        }
        
        protected virtual string GetCppType (string t)
        {
            // make sure there are no modifiers in the type string (add them back before returning)
            string typeToCompare = t;
            string[] splitType = null;
            if (t.Contains (' '))
            {
                splitType = t.Split (' ');
                typeToCompare = splitType[0];
                //for (int i = 1; i < splitType.Length; i++)
                //{
                //    var str = splitType[i];
                //    if (str == "modopt(System.Runtime.CompilerServices.IsLong)" && typeToCompare == "System.Int32")
                //        return "long";
                //    if (str == "modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte)" && typeToCompare == "System.SByte")
                //        return "char";
                //    //if (typeToCompare == "System.Byte")
                //    //    return "unsigned char";
                //    //if (typeToCompare == "System.SByte")
                //    //    return "signed char";
                //}

                foreach (var str in splitType)
                {
                    if (str == "modopt(System.Runtime.CompilerServices.IsLong)" && typeToCompare == "System.Int32")
                        return "long";
                    if (str == "modopt(System.Runtime.CompilerServices.IsSignUnspecifiedByte)" && typeToCompare == "System.SByte")
                        return "char";
                    //if (typeToCompare == "System.Byte")
                    //    return "unsigned char";
                    //if (typeToCompare == "System.SByte")
                    //    return "signed char";
                }

            }
            
            switch (typeToCompare)
            {
                case "System.Byte": typeToCompare = "System::Byte"; break;
                case "System.SByte": typeToCompare = "System::SByte"; break;
                case "System.Int16": typeToCompare = "short"; break;
                case "System.Int32": typeToCompare = "int"; break;
                case "System.Int64": typeToCompare = "long"; break;

                case "System.UInt16": typeToCompare = "System::UInt16"; break;
                case "System.UInt32": typeToCompare = "System::UInt32"; break;
                case "System.UInt64": typeToCompare = "System::UInt64"; break;

                case "System.Single": typeToCompare = "float"; break;
                case "System.Double": typeToCompare = "double"; break;
                case "System.Decimal": typeToCompare = "System::Decimal"; break;
                case "System.Boolean": typeToCompare = "bool"; break;
                case "System.Char": typeToCompare = "char"; break;
                case "System.Void": typeToCompare = "void"; break;
                case "System.String": typeToCompare = "System::String"; break;
                case "System.Object": typeToCompare = "System::Object"; break;
            }

            if (splitType != null)
            {
                // re-add modreq/modopt if it was there
                splitType[0] = typeToCompare;
                typeToCompare = string.Join (" ", splitType);
            }
            return typeToCompare == t ? null : typeToCompare;
        }

        protected override StringBuilder AppendTypeName (StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            string typeFullName = type.FullName;
            if (string.IsNullOrWhiteSpace (typeFullName))
                return buf;

            string cppType = GetCppType(typeFullName);
            if (cppType != null)
            {
                return buf.Append(cppType);
            }

            return base.AppendTypeName(buf, type, context);
        }
       
        public override string GetDeclaration(TypeReference tref)
        {
             if (!IsSupported(tref))
                return null;
             
            TypeDefinition def = tref.Resolve();
            return def != null 
                ? GetTypeDeclaration(def) 
                : GetTypeNameWithOptions(tref, !AppendHatOnReturn, false);
        }

        protected override string GetTypeDeclaration (TypeDefinition type)
        {
            string visibility = GetTypeVisibility (type.Attributes);
            if (visibility == null)
                return null;

            StringBuilder buf = new StringBuilder ();
            
            var genericParamList = GetTypeSpecifiGenericParameters(type);

            if (!visibility.Contains(":"))
            {
                AppendGenericItem(buf, genericParamList);
                AppendGenericTypeConstraints(buf, type);

            }

            buf.Append(visibility);
            buf.Append(" ");

            if (visibility.Contains(":"))
            {
                AppendGenericItem(buf, genericParamList);
                AppendGenericTypeConstraints(buf, type);
            }

            CppFullMemberFormatter full = new CppFullMemberFormatter (this.TypeMap);

            if (DocUtils.IsDelegate (type))
            {
                buf.Append("delegate ");
                MethodDefinition invoke = type.GetMethod ("Invoke");
                buf.Append(full.GetNameWithOptions(invoke.ReturnType));
                buf.Append(" ");
                buf.Append(GetNameWithOptions(type, false, false));
                AppendParameters (buf, invoke, invoke.Parameters);
                buf.Append(";");

                return buf.ToString ();
            }
            
            buf.Append(GetTypeKind (type));
            buf.Append(" ");
            var cppType = GetCppType(type.FullName);
            buf.Append(cppType == null
                    ? GetNameWithOptions(type, false, false)
                    : cppType);

            if (type.IsAbstract && !type.IsInterface)
                buf.Append(" abstract");
            if (type.IsSealed && !DocUtils.IsDelegate(type) && !type.IsValueType)
                buf.Append(" sealed");

            if (!type.IsEnum)
            {
                TypeReference basetype = type.BaseType;
                if (basetype != null && basetype.FullName == "System.Object" || type.IsValueType)   // FIXME
                    basetype = null;

                List<string> interfaceNames = DocUtils.GetUserImplementedInterfaces (type)
                        .Select (iface => full.GetNameWithOptions(iface, true, false))
                        .OrderBy (s => s)
                        .ToList ();

                if (basetype != null || interfaceNames.Count > 0)
                    buf.Append(" : ");

                if (basetype != null)
                {
                    buf.Append(full.GetNameWithOptions(basetype, true, false));
                    if (interfaceNames.Count > 0)
                        buf.Append(", ");
                }

                for (int i = 0; i < interfaceNames.Count; i++)
                {
                    if (i != 0)
                        buf.Append(", ");
                    buf.Append(interfaceNames[i]);
                }
            }

            return buf.ToString ();
        }

        protected virtual IList<GenericParameter> GetTypeSpecifiGenericParameters(TypeDefinition type)
        {
            var returnItems = new Collection<GenericParameter>();

            List<TypeReference> decls = DocUtils.GetDeclaringTypes(type);
            List<TypeReference> genArgs = GetGenericArguments(type);
            int argIndex = 0;
            int previouslyAppliedCount = 0;
            
            var lastItem = decls.Last();

            foreach (var decl in decls)
            {
                TypeReference declDef = decl.Resolve() ?? decl;

                int argumentCount = DocUtils.GetGenericArgumentCount(declDef);
                int countLeftUnupplied = argumentCount - previouslyAppliedCount;
                previouslyAppliedCount = argumentCount;

                if (decl != lastItem)
                {
                    argIndex = argIndex + argumentCount;
                }

                if (countLeftUnupplied > 0 && decl==lastItem)
                {

                    for (int i = 0; i < countLeftUnupplied; ++i)
                    {
                        returnItems.Add((GenericParameter)genArgs[argIndex++]);
                    }
                }
            }

            return returnItems;
        }

        protected virtual string GetTypeKind (TypeDefinition t)
        {
            if (t.IsEnum)
                return "enum class";
            if (t.IsValueType)
                //pure struct is unmanaged so cannot be used in managed context
                return "value class";
            if (t.IsClass || t.FullName == "System.Enum")
                return "ref class";
            if (t.IsInterface)
                return "interface class";
            throw new ArgumentException (t.FullName);
        }

        protected virtual string GetTypeNameWithOptions(TypeReference type, bool appendHat, bool appendGeneric = true)
        {
            var typeName = GetTypeName(type, EmptyAttributeParserContext.Empty(), appendGeneric);
            var hatTypeName =
                !type.IsByReference && !type.IsPointer
                    ? AppendHat(typeName, type, appendHat)
                    : typeName;

            return hatTypeName;
        }

        protected virtual string AppendHat(string stringToApply, TypeReference type, bool appendHat = true)
        {
            var buffer = new StringBuilder(stringToApply);
            AppendHat(buffer, type, appendHat);

            return buffer.ToString();
        }

        protected virtual StringBuilder AppendHat(StringBuilder buffer, TypeReference type, bool appendHat = true)
        {
            //no hat for value type with modopt (like short, which is represented as Int32 with modopt value )
            if (!type.IsArray && type.GetElementType().IsValueType && type.FullName.Contains("modopt"))
                return buffer;

            if (type is PointerType || type is ByReferenceType)
            {
                var typeToCheck = type is TypeSpecification
                    ? ((TypeSpecification)type).ElementType
                    :type.GetElementType();
                if (!typeToCheck.IsValueType
                    && !typeToCheck.IsPointer
                    && !NoHatTypes.Contains(typeToCheck.FullName)
                    //check for generic type
                    && !NoHatTypes.Contains(typeToCheck.GetType().FullName)
                )
                {
                    buffer.Append(HatModifier);
                }
                return buffer;
            }
            
            if ( !type.IsValueType 
                //is checked to skip hat for type declaration
                && appendHat
                //check for standart types
                && !NoHatTypes.Contains(type.FullName) 
                //check for generic type
                && !NoHatTypes.Contains(type.GetType().FullName)
                
                )
            {
               //add handler for reference types to have managed context
               buffer.Append(HatModifier);
            }

            return buffer;
        }

        protected virtual string GetTypeVisibility (TypeAttributes ta)
        {
            switch (ta & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.Public:
                    return "public";
                case TypeAttributes.NestedPublic:
                    return "public:";

                case TypeAttributes.NestedFamORAssem:
                    return "public protected";
                case TypeAttributes.NestedFamily:
                    return "protected:";

                default:
                    return null;
            }
        }

        protected override StringBuilder AppendGenericTypeConstraints (StringBuilder buf, TypeReference type)
        {
            if (type.GenericParameters.Count == 0)
                return buf;
            return AppendConstraints (buf, type.GenericParameters);
        }
        
        private StringBuilder AppendConstraints (StringBuilder buf, IList<GenericParameter> genArgs)
        {
            foreach (GenericParameter genArg in genArgs)
            {
                GenericParameterAttributes attrs = genArg.Attributes;
#if NEW_CECIL
                Mono.Collections.Generic.Collection<GenericParameterConstraint> constraints = genArg.Constraints;
#else
                IList<TypeReference> constraints = genArg.Constraints;
#endif
                if (attrs == GenericParameterAttributes.NonVariant && constraints.Count == 0)
                    continue;

                bool isref = (attrs & GenericParameterAttributes.ReferenceTypeConstraint) != 0;
                bool isvt = (attrs & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
                bool isnew = (attrs & GenericParameterAttributes.DefaultConstructorConstraint) != 0;
                bool comma = false;

                if (!isref && !isvt && !isnew && constraints.Count == 0)
                    continue;
                buf.Append(" where ").Append(genArg.Name).Append(" : ");
                if (isref)
                {
                    buf.Append("class");
                    comma = true;
                }
                else if (isvt)
                {
                    buf.Append("value class");
                    comma = true;
                }
                if (constraints.Count > 0 && !isvt)
                {
                    if (comma)
                        buf.Append(", ");

#if NEW_CECIL
                    buf.Append(GetTypeName (constraints[0].ConstraintType));
                    for (int i = 1; i < constraints.Count; ++i)
                        buf.Append(", ").Append(GetTypeName (constraints[i].ConstraintType));
#else
                    buf.Append(GetTypeName (constraints[0]));
                    for (int i = 1; i < constraints.Count; ++i)
                        buf.Append(", ").Append(GetTypeName (constraints[i]));
#endif
                }
                if (isnew && !isvt)
                {
                    if (comma)
                        buf.Append(", ");
                    buf.Append("gcnew()");
                }
            }
            return buf;
        }

        protected override string GetConstructorDeclaration (MethodDefinition constructor)
        {
            StringBuilder buf = new StringBuilder ();
            AppendVisibility (buf, constructor);
            

            buf.Append(' ');

            if (constructor.IsStatic)
                buf.Append("static ");

            base.AppendTypeName(buf, constructor.DeclaringType.Name);
            AppendParameters(buf, constructor, constructor.Parameters);
            buf.Append(';');

            return buf.ToString ();
        }

        protected override string GetMethodDeclaration (MethodDefinition method)
        {
            if (method.HasCustomAttributes && method.CustomAttributes.Any(
                    ca => ca.GetDeclaringType() == "System.Diagnostics.Contracts.ContractInvariantMethodAttribute"))
                return null;

            // Special signature for destructors.
            if (method.Name == "Finalize" && method.Parameters.Count == 0)
                return GetFinalizerName(method);

            StringBuilder buf = new StringBuilder();

            AppendVisibility(buf, method);
            AppendGenericMethod(buf, method);
            AppendGenericMethodConstraints(buf, method);

            if (DocUtils.IsExtensionMethod(method))
            {
                //no notion of Extension method; needs to mark with attribute and call as standard static method
                buf.Append("[System::Runtime::CompilerServices::Extension]").Append(GetLineEnding());
            }

            AppendModifiers(buf, method);

            if (buf.Length != 0)
                buf.Append(" ");

            buf.Append(GetTypeNameWithOptions(method.ReturnType, AppendHatOnReturn)).Append(" ");

            AppendMethodName(buf, method);
            AppendParameters(buf, method, method.Parameters);
            AppendExplisitImplementationMethod(buf, method);

            return buf.Append(";").ToString();
        }

        protected virtual StringBuilder AppendExplisitImplementationMethod(StringBuilder buf, MethodDefinition method)
        {
            if (method.HasOverrides)
            {
                //apply logic for explicit implementation om interface methods
                var interfaceMethodReference = method.Overrides.FirstOrDefault();
                buf.Append(" = ");
                buf.Append(GetTypeNameWithOptions(interfaceMethodReference?.DeclaringType, !AppendHatOnReturn))
                    .Append(NestedTypeSeparator);
                buf.Append(interfaceMethodReference?.Name);
            }
            return buf;
        }

        protected override StringBuilder AppendMethodName(StringBuilder buf, MethodDefinition method)
        {
            if (!method.Name.StartsWith("op_", StringComparison.Ordinal))
                return base.AppendMethodName(buf, method);

            // this is an operator
            switch (method.Name)
            {
                case "op_Implicit":
                case "op_Explicit":
                    buf.Length--; // remove the last space, which assumes a member name is coming
                    return buf;
                case "op_Addition":
                case "op_UnaryPlus":
                    return buf.Append("operator +");
                case "op_Subtraction":
                case "op_UnaryNegation":
                    return buf.Append("operator -");
                case "op_Division":
                    return buf.Append("operator /");
                case "op_Multiply":
                    return buf.Append("operator *");
                case "op_Modulus":
                    return buf.Append("operator %");
                case "op_BitwiseAnd":
                    return buf.Append("operator &");
                case "op_BitwiseOr":
                    return buf.Append("operator |");
                case "op_ExclusiveOr":
                    return buf.Append("operator ^");
                case "op_LeftShift":
                    return buf.Append("operator <<");
                case "op_RightShift":
                    return buf.Append("operator >>");
                case "op_LogicalNot":
                    return buf.Append("operator !");
                case "op_OnesComplement":
                    return buf.Append("operator ~");
                case "op_Decrement":
                    return buf.Append("operator --");
                case "op_Increment":
                    return buf.Append("operator ++");
                case "op_True":
                    return buf.Append("operator true");
                case "op_False":
                    return buf.Append("operator false");
                case "op_Equality":
                    return buf.Append("operator ==");
                case "op_Inequality":
                    return buf.Append("operator !=");
                case "op_LessThan":
                    return buf.Append("operator <");
                case "op_LessThanOrEqual":
                    return buf.Append("operator <=");
                case "op_GreaterThan":
                    return buf.Append("operator >");
                case "op_GreaterThanOrEqual":
                    return buf.Append("operator >=");
                default:
                    return base.AppendMethodName(buf, method);
            }

        }

        protected override StringBuilder AppendGenericMethodConstraints (StringBuilder buf, MethodDefinition method)
        {
            if (method.GenericParameters.Count == 0)
                return buf;
            return AppendConstraints (buf, method.GenericParameters);
        }

        protected override StringBuilder AppendGenericType(StringBuilder buf, TypeReference type, IAttributeParserContext context, bool appendGeneric = true, bool useTypeProjection = false, bool isTypeofOperator = false)
        {
            List<TypeReference> decls = DocUtils.GetDeclaringTypes(
                type is GenericInstanceType ? type.GetElementType() : type);
            List<TypeReference> genArgs = GetGenericArguments(type);
            int argIndex = 0;
            int previouslyAppliedCount = 0;
            bool insertNested = false;
            foreach (var decl in decls)
            {
                TypeReference declDef;
                try
                {
                    declDef = decl.Resolve() ?? decl; 
                }
                catch
                {
                    //Resolve() can fail as sometimes Cecil understands types as .net 
                    //needs to ignore those errors
                    declDef = decl;
                }

                if (insertNested)
                {
                    buf.Append(NestedTypeSeparator);
                }
                insertNested = true;
                AppendTypeName(buf, declDef, context);
                int argumentCount = DocUtils.GetGenericArgumentCount(declDef);
                int countLeftUnapplied = argumentCount - previouslyAppliedCount;
                previouslyAppliedCount = argumentCount;
                var lastItem = decls.Last();
                if (countLeftUnapplied > 0 
                    && (appendGeneric
                    //this is to add generic syntax for parent classes in declaration of nested class
                    //eg, ref class MyList<T>::Helper -> needs to add <T> to MyList class
                    || decls.Count>=2 && decl != lastItem)
                    )
                {
                    buf.Append(GenericTypeContainer[0]);
                    var origState = MemberFormatterState;
                    MemberFormatterState = MemberFormatterState.WithinGenericTypeParameters;

                    var item = genArgs[argIndex++];
                    _AppendTypeName(buf, item, context, useTypeProjection: useTypeProjection);
                    if (declDef.GenericParameters.All(x => x.FullName != item.FullName))
                    {
                        AppendHat(buf, item, AppendHatOnReturn);
                    }

                    for (int i = 1; i < countLeftUnapplied; ++i)
                    {
                        var newItem = genArgs[argIndex++];
                        _AppendTypeName(buf.Append(", "), newItem, context);
                        if (declDef.GenericParameters.All(x => x.FullName != newItem.FullName))
                        {
                            //add hat only for non-generic types
                            AppendHat(buf, newItem);
                        }
                    }
                    MemberFormatterState = origState;
                    buf.Append(GenericTypeContainer[1]);
                }
            }
            return buf;
        }

        protected override string GetFinalizerName (MethodDefinition method)
        {
            //~classname() { }   // destructor
            //!classname() { }   // finalizer
            return "!" + method.DeclaringType.Name + " ()";
        }

        protected override StringBuilder AppendVisibility (StringBuilder buf, MethodDefinition method)
        {
            if (method == null)
                return buf;
            if (method.IsPublic)
                return buf.Append ("public:").Append(GetLineEnding());
            if (method.IsFamily )
                return buf.Append ("protected:").Append(GetLineEnding());
            if(method.IsFamilyOrAssembly)
                return buf.Append("protected public:").Append(GetLineEnding());
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
            TypeDefinition declType = method.DeclaringType;
            if (method.IsAbstract && !declType.IsInterface) modifiers += " abstract";

            modifiers = AppendSealedModifiers(modifiers, method);
            
            switch (method.Name)
            {
                case "op_Implicit":
                    modifiers += " operator";
                    break;
                case "op_Explicit":
                    modifiers += " explicit operator";
                    break;
            }

            return buf.Append (modifiers);
        }

        protected virtual string AppendSealedModifiers(string modifiersString, MethodDefinition method)
        {
            //no need to apply sealed here as virtual keyword is used for interface implementation even for sealed classes
            return modifiersString;
        }

        protected override StringBuilder AppendGenericMethod (StringBuilder buf, MethodDefinition method)
        {
            if (method.IsGenericMethod ())
            {
                IList<GenericParameter> args = method.GenericParameters;
                AppendGenericItem(buf, args);
            }
            return buf;
        }

        protected virtual StringBuilder AppendGenericItem(StringBuilder buf, IList<GenericParameter> args)
        {
            if (args!=null && args.Any())
            {
                buf.Append("generic <typename ");
                buf.Append(args[0].Name);
                for (int i = 1; i < args.Count; ++i)
                    buf.Append(", typename ").Append(args[i].Name);
                buf.Append(">");
                buf.Append(GetLineEnding());
            }
            return buf;
        }

        protected override StringBuilder AppendParameters (StringBuilder buf, MethodDefinition method, IList<ParameterDefinition> parameters)
        {
            return AppendParameters (buf, parameters, '(', ')');
        }

        private StringBuilder AppendParameters (StringBuilder buf,IList<ParameterDefinition> parameters, char? begin, char? end)
        {
            buf.Append (begin);

            if (parameters.Count > 0)
            {
                AppendParameter (buf, parameters[0]);
                for (int i = 1; i < parameters.Count; ++i)
                {
                    buf.Append (", ");
                    AppendParameter (buf, parameters[i]);
                }
            }

            return buf.Append (end);
        }

        protected virtual StringBuilder AppendParameter (StringBuilder buf, ParameterDefinition parameter)
        {
           if (parameter.ParameterType is ByReferenceType)
            {
                if (parameter.IsOut)
                {
                    //no notion of out -> mark with attribute to distinguish in other languages 
                    buf.Append("[Runtime::InteropServices::Out] ");
                }
            }

            if (IsParamsParameter(parameter))
                buf.AppendFormat ("... ");
            
            buf.Append(GetTypeNameWithOptions(parameter.ParameterType, AppendHatOnReturn));
            if (!buf.ToString().EndsWith(" "))
                buf.Append(" ");

            buf.Append(parameter.Name);
            
            return buf;
        }

        protected bool IsParamsParameter(ParameterDefinition parameter)
        {
            if (parameter.HasCustomAttributes)
            {
                var isParams = parameter.CustomAttributes.Any(ca => ca.AttributeType.Name == "ParamArrayAttribute");
                if (isParams)
                    return true;
            }
            return false;
        }

        protected override StringBuilder AppendArrayModifiers(StringBuilder buf, ArrayType array)
        {
            return buf;
        }

        protected override StringBuilder AppendArrayTypeName(StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            buf.Append("cli::array <");

            var item = type is TypeSpecification spec ? spec.ElementType : type.GetElementType();
            _AppendTypeName(buf, item, context);
            AppendHat(buf, item);

            if (type is ArrayType arrayType)
            {
                int rank = arrayType.Rank;
                if (rank > 1)
                {
                    buf.AppendFormat(", {0}", rank);
                }
            }
            
            buf.Append(">");

            return buf;
        }

        protected override StringBuilder AppendRefTypeName(StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            TypeSpecification spec = type as TypeSpecification;
            _AppendTypeName(buf, spec != null ? spec.ElementType : type.GetElementType(), context);
            AppendHat(buf, type);
            buf.Append(RefTypeModifier);

            return buf;
        }

        protected override StringBuilder AppendPointerTypeName(StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            TypeSpecification spec = type as TypeSpecification;
             _AppendTypeName(buf, spec != null ? spec.ElementType : type.GetElementType(), context);
            AppendHat(buf, type);
            buf.Append(PointerModifier);
            return buf;
        }

        protected override string GetPropertyDeclaration (PropertyDefinition property)
        {
            var propVisib = GetPropertyVisibility(property, out var getVisible, out var setVisible);

            var buf=new StringBuilder();
            buf.Append(propVisib);
            
            // Pick an accessor to use for static/virtual/override/etc. checks.
            var method = property.SetMethod ?? property.GetMethod;

            string modifiers = String.Empty;
            if (method.IsStatic) modifiers += " static";
            if (method.IsVirtual && !method.IsAbstract)
            {
                if (((method.Attributes & MethodAttributes.SpecialName) != 0))
                    modifiers += " virtual";
                else
                    modifiers += " override";
            }
            TypeDefinition declDef = (TypeDefinition)method.DeclaringType;
            if (method.IsAbstract && !declDef.IsInterface)
                modifiers += " abstract";
            if (method.IsFinal)
                modifiers += " sealed";
            if (modifiers == " virtual sealed")
                modifiers = "";
            buf.Append(modifiers).Append(' ').Append("property ");

            var typeName = GetTypeNameWithOptions(property.PropertyType, AppendHatOnReturn);

            buf.Append(typeName).Append(' ');

            IEnumerable<MemberReference> defs = property.DeclaringType.GetDefaultMembers();
            string propertyName = property.Name;
            foreach (MemberReference mi in defs)
            {
                if (mi == property)
                {
                    propertyName = "default";
                    break;
                }
            }
            buf.Append(propertyName == "default" ? propertyName : DocUtils.GetPropertyName(property, NestedTypeSeparator));

            bool hasParams=false;
            if (property.Parameters.Count != 0)
            {
                hasParams = true;
                buf.Append('[');
                    buf.Append(GetTypeNameWithOptions(property.Parameters[0].ParameterType, AppendHatOnReturn));
                    for (int i = 1; i < property.Parameters.Count; ++i)
                    {
                        buf.Append(", ");
                        buf.Append(GetTypeNameWithOptions(property.Parameters[i].ParameterType, AppendHatOnReturn));
                    }
                buf.Append(']');
            }

            buf.Append(" { ");
            if (getVisible != null)
            {
                if (getVisible != propVisib)
                    buf.Append(' ').Append(getVisible);
                buf.AppendFormat("{0} get", typeName);

                if (hasParams) AppendParameters(buf, property.Parameters, '(', ')');
                else buf.Append("()");

                buf.Append(";");
            }
            if (setVisible != null)
            {
                if (setVisible != propVisib)
                    buf.Append(' ').Append(setVisible);
                buf.Append(' ').AppendFormat("void set(");

                if (hasParams)
                {//no need for braces since they are added in other place
                    AppendParameters(buf, property.Parameters, null, null);
                    buf.Append(", ");
                }
                buf.AppendFormat("{0} value)", typeName);
                buf.Append(";");
            }
            buf.Append(" };");

            return buf[0] != ' ' ? buf.ToString () : buf.ToString (1, buf.Length - 1);
        }

        protected virtual string GetPropertyVisibility(PropertyDefinition property, out string getVisible, out string setVisible )
        {
            getVisible = null;
            setVisible = null;

            if (DocUtils.IsAvailablePropertyMethod(property.GetMethod))
                getVisible = AppendVisibility(new StringBuilder(), property.GetMethod).ToString();
            if (DocUtils.IsAvailablePropertyMethod(property.SetMethod))
                setVisible = AppendVisibility(new StringBuilder(), property.SetMethod).ToString();

            if (setVisible == null && getVisible == null)
                return null;
            
            StringBuilder buf = new StringBuilder();
            if (getVisible != null && (setVisible == null || getVisible == setVisible))
                buf.Append(getVisible);
            else if (setVisible != null && getVisible == null)
                buf.Append(setVisible);
            else
                buf.Append("public: ");

            return buf.ToString();
        }

        protected override string GetFieldDeclaration (FieldDefinition field)
        {
            TypeDefinition declType = field.DeclaringType;
            if (declType.IsEnum && field.Name == "value__")
                return null; // This member of enums aren't documented.

            StringBuilder buf = new StringBuilder ();
            AppendFieldVisibility (buf, field);
            
            if (declType.IsEnum)
                return field.Name;

            if (field.IsStatic && !field.IsLiteral)
                buf.Append("static ");
            if (field.IsInitOnly)
                buf.Append("initonly ");

            string fieldFullName = field.FullName;
            if (fieldFullName.Contains(' '))
            {
                var splitType = fieldFullName.Split(' ');

                if (splitType.Any(str => str == "modopt(System.Runtime.CompilerServices.IsConst)"))
                {
                    buf.Append("const ");
                }
            }
            
            buf.Append(GetTypeNameWithOptions(field.FieldType, AppendHatOnReturn)).Append(' ');
            buf.Append(field.Name);
            AppendFieldValue (buf, field);
            buf.Append(';');

            return buf.ToString ();
        }

        protected virtual StringBuilder AppendFieldVisibility (StringBuilder buf, FieldDefinition field)
        {
            if (field.IsPublic)
                return buf.Append("public: ");
            if (field.IsFamily)
                return buf.Append("protected: ");
            if(field.IsFamilyOrAssembly)
                return buf.Append("protected public: ");
            return buf;
        }

        protected static StringBuilder AppendFieldValue (StringBuilder buf, FieldDefinition field)
        {
            // enums have a value__ field, which we ignore
            if (field.DeclaringType.IsEnum ||
                    field.DeclaringType.IsGenericType ())
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
                    buf.Append(" = ").Append("nullptr");
                else if (val is Enum)
                    buf.Append(" = ").Append(val);
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
                    buf.Append(" = ").Append(value);
                }
            }
            return buf;
        }

        protected override string GetEventDeclaration (EventDefinition e)
        {
            StringBuilder buf = new StringBuilder ();
            if (AppendVisibility (buf, e.AddMethod).Length == 0)
            {
                return null;
            }

            AppendModifiers(buf, e.AddMethod);

            buf.Append(" event ");

            var typeName = GetTypeNameWithOptions(e.EventType, AppendHatOnReturn);

            buf.Append(typeName).Append(' ');
            buf.Append(e.Name).Append(';');
            
            return buf.ToString ();
        }
        
        public virtual string GetNameWithOptions(MemberReference member, bool appendGeneric = true, bool appendHat = true)
        {
            if (member is TypeReference type)
                return GetTypeNameWithOptions(type, appendHat, appendGeneric);
            var method = member as MethodReference;
            if (method != null && method.Name == ".ctor") // method.IsConstructor
                return GetConstructorName(method);
            if (method != null)
                return GetMethodName(method);
            if (member is PropertyReference prop)
                return GetPropertyName(prop);
            if (member is FieldReference field)
                return GetFieldName(field);
            if (member is EventReference e)
                return GetEventName(e);
            throw new NotSupportedException("Can't handle: " +
                                            (member?.GetType().ToString() ?? "null"));
        }
        
        public override bool IsSupported(MemberReference mref)
        {
            if (mref.IsDefinition == false)
                mref = mref.Resolve() as MemberReference;
            
            if (mref is FieldDefinition)
                return IsSupportedField((FieldDefinition)mref);
            else if (mref is MethodDefinition)
                return IsSupportedMethod((MethodDefinition)mref);
            else if (mref is PropertyDefinition)
                return IsSupportedProperty((PropertyDefinition)mref);
            else if (mref is EventDefinition)
                return IsSupportedEvent((EventDefinition)mref);
            else if (mref is AttachedPropertyDefinition || mref is AttachedEventDefinition)
                return false;

            throw new NotSupportedException("Unsupported member type: " + mref?.GetType().FullName);
        }
        
        public virtual bool IsSupportedMethod(MethodDefinition mdef)
        {
            return
                IsSupported(mdef.ReturnType)
                && mdef.Parameters.All(i => IsSupported(i.ParameterType))
                //no possibility for default parameters
                && mdef.Parameters.All(i => !i.HasDefault && !i.IsOptional && !i.HasConstant)
                ;
        }
        public virtual bool IsSupportedField(FieldDefinition fdef)
        {
            return IsSupported(fdef.FieldType);
        }
        public virtual bool IsSupportedProperty(PropertyDefinition pdef)
        {
            return IsSupported(pdef.PropertyType);
        }

        public virtual bool IsSupportedEvent(EventDefinition edef)
        {
            return IsSupported(edef.EventType);
        }

        public bool HasNestedClassesDuplicateNames(TypeReference tref)
        {
            string inputName = DocUtils.GetFormattedTypeName(tref.Name);
            TypeDefinition parentType;
            try
            {
                 parentType = tref.DeclaringType?.Resolve();
            }
            catch
            {
                //Resolve() can fail as sometimes Cecil understands types as .net 
                //needs to ignore those errors
                 parentType = null;
            }

            if (parentType != null && parentType.HasNestedTypes)
            {
                var listOfNested = parentType.NestedTypes;
                int count = listOfNested.Select(x => DocUtils.GetFormattedTypeName(x.Name)).Count(y => y == inputName);
                if (count > 1)
                    return true;
            }
            return false;
        }

        public override bool IsSupported(TypeReference tref)
        {
          
            return !HasNestedClassesDuplicateNames(tref) 
                && IsSupportedGenericParameter(tref) 
                && base.IsSupported(tref)
                ;
        }

        protected bool IsSupportedGenericParameter(TypeReference tref)
        {
            if (tref is GenericParameter parameter)
            {
                //no support for parameter covariance/contrvariance
                GenericParameterAttributes attrs = parameter.Attributes;
                bool isout = (attrs & GenericParameterAttributes.Covariant) != 0;
                bool isin = (attrs & GenericParameterAttributes.Contravariant) != 0;
                if (isin || isout)
                    return false;
            }

            return true;
        }
    }
}
