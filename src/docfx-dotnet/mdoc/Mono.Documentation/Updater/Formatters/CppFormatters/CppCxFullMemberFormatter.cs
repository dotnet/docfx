using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Documentation.Util;

namespace Mono.Documentation.Updater.Formatters.CppFormatters
{
    public class CppCxFullMemberFormatter : CppFullMemberFormatter
    {
        public override string Language => Consts.CppCx;
        protected override string RefTypeModifier => " & ";

        protected readonly IEnumerable<string> ValueClassPropertyTypeAllowed = new List<string>()
        {
            "System.String",
            "Platform.IBox<T>",
            //fundamental numeric types
            "System.SByte",
            "System.Byte",
            "System.Int16",
            "System.UInt16",
            "System.Int32",
            "System.UInt32",
            "System.Int64",
            "System.UInt64",
            "System.Single",
            "System.Double",
            "System.Decimal"
        };

        protected readonly IEnumerable<string> CustomAttributesFieldTypesAllowed = new List<string>()
        {
            "System.Int32",
            "System.UInt32",
            "System.Boolean",
            "System.String",
            "Windows.Foundation.HResult",
            "Platform.Type"
        };

        protected static readonly IEnumerable<string> AllowedFundamentalTypes = new List<string>()
        {
            //fundamental numeric types
            "System.Byte",
            "System.Int16",
            "System.UInt16",
            "System.Int32",
            "System.UInt32",
            "System.Int64",
            "System.UInt64",
            "System.Single",
            "System.Double",
            "System.SByte",
            //other fundamental types
            "System.Object",
            "System.Boolean",
            "System.Char",
            "System.Void",
            "System.String",
            "System.ValueType",
            "System.Enum",
            "System.Guid",
        };

        protected virtual IList<string> GetAllowedTypes()
        {
            return new List<string>(AllowedFundamentalTypes)
            {
                "System.Delegate",
                "System.MulticastDelegate",
                "System.Type",
                "System.Attribute"
            };

        }
        protected readonly IEnumerable<string> CppCxSpecificNamespases = new List<string>()
        {
            "Platform",
            "Platform.Collections",
            "Platform.Collections.Details",
            "Platform.Details",
            "Platform.Metadata",
            "Platform.Runtime.CompilerServices",
            "Platform.Runtime.InteropServices",
            "Windows.Foundation.Collections"
        };

        public CppCxFullMemberFormatter() : this(null) {}
        public CppCxFullMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendNamespace(StringBuilder buf, TypeReference type)
        {
            string ns = DocUtils.GetNamespace(type, NestedTypeSeparator);
            if (GetCppType(type.FullName) == null && !string.IsNullOrEmpty(ns) && ns != "System")
                buf.Append(ns).Append(NestedTypeSeparator);
            return buf;
        }

        protected override string GetCppType(string t)
        {
            // make sure there are no modifiers in the type string (add them back before returning)
            string typeToCompare = t;
            string[] splitType = null;
            if (t.Contains(' '))
            {
                splitType = t.Split(' ');
                typeToCompare = splitType[0].Trim('&');
            }

            switch (typeToCompare)
            {
                case "System.Byte": typeToCompare = "byte"; break;
                case "System.Int16": typeToCompare = "short"; break;
                case "System.Int32": typeToCompare = "int"; break;
                case "System.Int64": typeToCompare = "long long"; break;

                case "System.UInt16": typeToCompare = "unsigned short"; break;
                case "System.UInt32": typeToCompare = "unsigned int"; break;
                case "System.UInt64": typeToCompare = "unsigned long long"; break;

                case "System.Single": typeToCompare = "float"; break;
                case "System.Double": typeToCompare = "double"; break;
                case "System.Boolean": typeToCompare = "bool"; break;
                case "System.Char": typeToCompare = "char16"; break;
                case "System.Void": typeToCompare = "void"; break;
                case "System.Guid": typeToCompare = "Platform::Guid"; break;
                case "System.String": typeToCompare = "Platform::String"; break;
                case "System.Object": typeToCompare = "Platform::Object"; break;
                case "System.Type": typeToCompare = "Platform::Type"; break;
                case "System.Attribute": typeToCompare = "Platform::Metadata::Attribute"; break;
                case "Windows.Foundation.Numerics.Matrix3x2": typeToCompare = "float3x2"; break;
                case "Windows.Foundation.Numerics.Matrix4x4": typeToCompare = "float4x4"; break;
                case "Windows.Foundation.Numerics.Plane": typeToCompare = "plane"; break;
                case "Windows.Foundation.Numerics.Quaternion": typeToCompare = "quaternion"; break;
                case "Windows.Foundation.Numerics.Vector2": typeToCompare = "float2"; break;
                case "Windows.Foundation.Numerics.Vector3": typeToCompare = "float3"; break;
                case "Windows.Foundation.Numerics.Vector4": typeToCompare = "float4"; break;
            }

            if (splitType != null)
            {
                // re-add modreq/modopt if it was there
                splitType[0] = typeToCompare;
                typeToCompare = string.Join(" ", splitType);
            }
            return typeToCompare == t ? null : typeToCompare;
        }

        protected override StringBuilder AppendArrayTypeName(StringBuilder buf, TypeReference type,
            IAttributeParserContext context)
        {
            buf.Append("Platform::Array <");

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

        protected override string GetTypeDeclaration(TypeDefinition type)
        {
            string visibility = GetTypeVisibility(type.Attributes);
            if (visibility == null)
                return null;

            StringBuilder buf = new StringBuilder();

            if (!visibility.Contains(":"))
            {
                AppendWebHostHiddenAttribute(buf, type);
            }

            buf.Append(visibility);
            buf.Append(" ");

            if (visibility.Contains(":"))
            {
                AppendWebHostHiddenAttribute(buf, type);
            }

            CppFullMemberFormatter full = new CppCxFullMemberFormatter(this.TypeMap);

            if (DocUtils.IsDelegate(type))
            {
                buf.Append("delegate ");
                MethodDefinition invoke = type.GetMethod("Invoke");
                buf.Append(full.GetNameWithOptions(invoke.ReturnType)).Append(" ");
                buf.Append(GetNameWithOptions(type, false, false));
                AppendParameters(buf, invoke, invoke.Parameters);
                buf.Append(";");

                return buf.ToString();
            }

            buf.Append(GetTypeKind(type));
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

                List<string> interfaceNames;
                try
                {
                    //for c++/cx Resolve() can fail as Cecil understands CX types as .net (eg, "System.Attribute" instead of "Platform::Metadata::Attribute")
                    interfaceNames = DocUtils.GetUserImplementedInterfaces(type)
                        .Select(iface => full.GetNameWithOptions(iface, true, false))
                        .OrderBy(s => s)
                        .ToList();
                }
                catch
                {
                    interfaceNames = null;
                }

                if (basetype != null || interfaceNames?.Count > 0)
                    buf.Append(" : ");

                if (basetype != null)
                {
                    var appendValue = GetCppType(basetype.FullName);
                    buf.Append(appendValue ?? full.GetNameWithOptions(basetype, true, false));
                    if (interfaceNames?.Count > 0)
                        buf.Append(", ");
                }

                for (int i = 0; i < interfaceNames?.Count; i++)
                {
                    if (i != 0)
                        buf.Append(", ");
                    buf.Append(interfaceNames?[i]);
                }

            }

            return buf.ToString();
        }

        public void AppendWebHostHiddenAttribute(StringBuilder buf, TypeDefinition typeDef)
        {
            //public unsealed ref class needs to be marked with attribute
            //to ensure that is not visible to UWP apps that are written in JavaScript
            if (typeDef.IsClass && !typeDef.IsSealed && (typeDef.IsPublic || typeDef.IsNestedPublic) && typeDef.CustomAttributes.All(x => x.GetDeclaringType() != "Windows.Foundation.Metadata.WebHostHiddenAttribute"))
            {
                buf.Append("[Windows::Foundation::Metadata::WebHostHidden]").Append(GetLineEnding());
            }
        }

        protected override StringBuilder AppendExplisitImplementationMethod(StringBuilder buf, MethodDefinition method)
        {
            if(IsExplicitlyImplemented(method))
            {
                buf.Append(" = ");

                var interfaceMethodReference = method.Overrides.FirstOrDefault();

                buf.Append(GetTypeNameWithOptions(interfaceMethodReference?.DeclaringType, !AppendHatOnReturn, true))
                    .Append(NestedTypeSeparator);
                buf.Append(interfaceMethodReference?.Name);
            }
            return buf;
        }

        protected override string AppendSealedModifiers(string modifiersString, MethodDefinition method)
        {
            if (!IsExplicitlyImplemented(method))
            {
                if (method.IsFinal) modifiersString += " sealed";
                if (modifiersString == " virtual sealed") modifiersString = "";
            }

            return modifiersString;
        }

        private bool IsExplicitlyImplemented(MethodDefinition method)
        {
            if (!method.HasOverrides) return false;

            //apply logic for explicit implementation of interface methods
            var interfaceMethodReference = method.Overrides.FirstOrDefault();

            if (interfaceMethodReference != null
                //need to filter UWP specific interfaces, generated by default
                //eg from decompile, public sealed class Class1 : __IClass1PublicNonVirtuals, __IClass1ProtectedNonVirtuals
                && !Regex.IsMatch(interfaceMethodReference.DeclaringType.Name,
                    $"^.*{method.DeclaringType.Name}.*Virtual.*$"))
            {
                return true;
            }

            return false;
        }

        public override bool IsSupported(TypeReference tref)
        {
            if (HasNestedClassesDuplicateNames(tref))
                return false;

            var allowedTypes = GetAllowedTypes();
            
            //no support of jagged arrays
            if (tref is ArrayType)
            {
                var typeOfArray= tref is TypeSpecification spec ? spec.ElementType : tref.GetElementType();
                if (typeOfArray is ArrayType)
                {
                    return false;
                }

                if (allowedTypes.Contains(typeOfArray.FullName) ||
                    CppCxSpecificNamespases.Contains(typeOfArray.Namespace))
                {
                    return true;
                }
            }

            if (allowedTypes.Contains(tref.FullName.Split(' ')[0].Trim('&')) || CppCxSpecificNamespases.Contains(tref.Namespace))
            {
                return true;
            }

            var ns = DocUtils.GetNamespace(tref);

            var typedef = tref.Resolve();
            if (typedef != null)
            {
                if (DocUtils.IsDelegate(typedef))
                {
                    if (typedef.HasGenericParameters && typedef.IsPublic)
                        //generic delegates cannot be declared as public
                        return false;

                    return 
                        //check types of delegate signature
                        IsSupported(typedef.GetMethod("Invoke")) 
                        //check type
                        && base.IsSupported(tref) && ! (ns.StartsWith("System.") || ns.StartsWith("System"));
                }

                if (typedef.IsInterface && typedef.HasGenericParameters &&
                    typedef.GenericParameters.Any(x => x.HasConstraints 
                        || x.HasReferenceTypeConstraint 
                        || x.HasDefaultConstructorConstraint 
                        || x.HasNotNullableValueTypeConstraint)
                    )
                {
                    //generic interface - Type parameters cannot be constrained
                    return false;
                }

                if (HasUnsupportedParent(typedef))
                {
                    return false;
                }

                var typeVisibility = typedef.Attributes & TypeAttributes.VisibilityMask;

                if (typeVisibility == TypeAttributes.Public
                    //all public types, including those in your own code, must be declared in a namespace 
                    && (string.IsNullOrEmpty(ns)
                    //public standard C++ types are not allowed
                    || ns.StartsWith("std") || ns.StartsWith("cli"))
                )
                {
                    return false;
                }

                if (typeVisibility == TypeAttributes.NestedPublic
                    && (typedef.IsEnum || typedef.IsClass)
                )
                {
                    //no support of nested public classes/enums
                    return false;
                }
                
                if (typedef.IsClass && typeVisibility == TypeAttributes.Public && typedef.HasGenericParameters)
                {
                    //Public ref classes that have type parameters (generics) are not permitted.
                    return false;
                }

                if (typedef.IsClass && typeVisibility == TypeAttributes.Public)
                {
                    //A public ref class that has a public constructor must also be declared as sealed 
                    //to prevent further derivation through the application binary interface (ABI).
                    if (typedef.Methods.Any(x => x.IsConstructor && x.IsPublic) && !typedef.IsSealed)
                    {
                        return false;
                    }
                }

                if (typedef.IsValueType && !typedef.IsEnum && typedef.Fields.Any(x =>
                     !ValueClassPropertyTypeAllowed.Contains(x.FieldType.FullName) && !(x.FieldType.Resolve() != null && x.FieldType.Resolve().IsEnum)))
                {
                    //A value struct or value class can contain as fields only 
                    //fundamental numeric types, enum classes, Platform::String^, or Platform::IBox <T>^
                    return false;
                }


                bool condition;
                try
                {
                    //custom attribute can contain only public fields and only with allowed types or enums
                    condition = IsCustomAttribute(typedef)
                                && (typedef.Fields.Any(z =>
                                            !CustomAttributesFieldTypesAllowed.Contains(z.FieldType.FullName) 
                                            && !(z.FieldType.Resolve() != null && z.FieldType.Resolve().IsEnum)
                                            ) 
                                        || typedef.Properties.Count != 0 
                                        || typedef.Methods.Count(x => !x.IsConstructor) != 0 
                                        || typedef.Events.Count != 0
                                );
                }
                catch
                {
                    condition = false;
                }

                if (condition)
                {
                    //custom attribute can contain only public fields and only with allowed types or enums
                    return false;
                }
            }
           
            //cannot support .Net types
            return !ns.StartsWith("System.") && !ns.Equals("System") && base.IsSupported(tref);
        }

        protected bool HasUnsupportedParent(TypeDefinition typedef)
        {
            var collect = new List<TypeDefinition>();
            TypeReference baseTypeReference = typedef.BaseType;
            TypeDefinition basetypeDefenition = null;

            var allowedTypes = GetAllowedTypes();

            try
            {
                //iterate through all classes in in inheritance hierarhy to exclude usage of .net types
                basetypeDefenition = baseTypeReference?.Resolve();
                while (basetypeDefenition != null)
                {
                    if (allowedTypes.Contains(basetypeDefenition.FullName) || basetypeDefenition.BaseType == null)
                    {
                        break;
                    }

                    collect.Add(basetypeDefenition);

                    //needs to call Resolve to grab all base classes 
                    basetypeDefenition = basetypeDefenition.BaseType?.Resolve();
                }
            }
            catch (Exception)
            {
                //for c++/cx Resolve() can fail as Cecil understands types as .net (eg, "System.Attribute" instead of "Platform::Metadata::Attribute")
                //needs to ignore those errors
                baseTypeReference = basetypeDefenition?.BaseType ?? baseTypeReference;
            }

            if (collect.Any(x => DocUtils.GetNamespace(x).StartsWith("System.") || DocUtils.GetNamespace(x).Equals("System"))
                || !allowedTypes.Contains(baseTypeReference?.FullName)
                        && (
                            DocUtils.GetNamespace(baseTypeReference).StartsWith("System.")
                            || DocUtils.GetNamespace(baseTypeReference).Equals("System")
                           )
                )
                //can only be Windows Runtime types(no support for .net types)
                return true;

            try
            {
                IEnumerable<TypeReference> interfaceNames = DocUtils.GetUserImplementedInterfaces(typedef).ToList();

                if (interfaceNames.Any(x => DocUtils.GetNamespace(x).StartsWith("System.") || DocUtils.GetNamespace(x).Equals("System")))
                {
                    //can only be Windows Runtime types(no support for .net types)
                    return true;
                }
            }
            catch
            {
                //for c++/cx Resolve() can fail as Cecil understands types as .net (eg, "System.Attribute" instead of "Platform::Metadata::Attribute")
                //needs to ignore those errors
            }

            return false;
        }
      
        public override bool IsSupportedField(FieldDefinition fdef)
        {
            bool isEnumFieldType;
            try
            {
                var typedef = fdef.FieldType.Resolve();

                if (typedef != null)
                    isEnumFieldType = typedef.IsEnum;
                else
                    isEnumFieldType = false;
            }
            catch
            {
                //for c++/cx Resolve() can fail as Cecil understands types as .net (eg, "System.Attribute" instead of "Platform::Metadata::Attribute")
                //needs to ignore those errors
                isEnumFieldType = false;
            }

            if (IsCustomAttribute(fdef.DeclaringType))
            {
                return !CustomAttributesFieldTypesAllowed.Contains(fdef.FieldType.FullName) || 
                    !isEnumFieldType;
            }

            if (fdef.IsPublic && fdef.DeclaringType.IsClass && !fdef.DeclaringType.IsValueType && !fdef.DeclaringType.IsEnum)
            {
                //Public fields are not allowed in ref class
                return false;
            }

            //todo: no const members - which can be??
            //const member functions + pointers

            return base.IsSupportedField(fdef);
        }

        public override bool IsSupportedProperty(PropertyDefinition pdef)
        {
            var propVisibility = GetPropertyVisibility(pdef, out _,  out _);
            if ((propVisibility.Contains("public:")
                || propVisibility.Contains("protected:"))
                  &&  pdef.Parameters.Count > 0)
                //no support of public indexed property
                return false;

            return base.IsSupportedProperty(pdef);
        }

        public override bool IsSupportedMethod(MethodDefinition mdef)
        {
            if (mdef.Parameters.Any(IsParamsParameter))
            {
                return false;
            }

            return base.IsSupportedMethod(mdef);
        }

        private bool IsCustomAttribute(TypeDefinition typedef)
        {
            var containingTypeNamespace = DocUtils.GetNamespace(typedef);

            if (typedef.BaseType?.FullName == "System.Attribute" && !containingTypeNamespace.StartsWith("System.") && !containingTypeNamespace.StartsWith("System"))
            {
                return true;
            }

            return false;
        }
    }
}