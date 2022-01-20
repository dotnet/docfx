using System;
using System.Collections.Generic;
using System.Text;

using Mono.Cecil;

using Mono.Documentation.Util;

namespace Mono.Documentation.Updater
{
    class SlashDocMemberFormatter : MemberFormatter
    {
        protected override string[] GenericTypeContainer
        {

            get { return new string[] { "{", "}" }; }
        }

        protected bool AddTypeCount = true;

        protected TypeReference genDeclType;
        protected MethodReference genDeclMethod;

        public SlashDocMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendTypeName (StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            if (type is GenericParameter)
            {
                int l = buf.Length;
                if (genDeclType != null)
                {
                    IList<GenericParameter> genArgs = genDeclType.GenericParameters;
                    for (int i = 0; i < genArgs.Count; ++i)
                    {
                        if (genArgs[i].Name == type.Name)
                        {
                            buf.Append ('`').Append (i);
                            break;
                        }
                    }
                }
                if (genDeclMethod != null)
                {
                    IList<GenericParameter> genArgs = null;
                    if (genDeclMethod.IsGenericMethod ())
                    {
                        genArgs = genDeclMethod.GenericParameters;
                        for (int i = 0; i < genArgs.Count; ++i)
                        {
                            if (genArgs[i].Name == type.Name)
                            {
                                buf.Append ("``").Append (i);
                                break;
                            }
                        }
                    }
                }
                if ((genDeclType == null && genDeclMethod == null) || buf.Length == l)
                {
                    // Probably from within an explicitly implemented interface member,
                    // where CSC uses parameter names instead of indices (why?), e.g.
                    // MyList`2.Mono#DocTest#Generic#IFoo{A}#Method``1(`0,``0) instead of
                    // MyList`2.Mono#DocTest#Generic#IFoo{`0}#Method``1(`0,``0).
                    buf.Append (type.Name);
                }
            }
            else
            {
                base.AppendTypeName (buf, type, context);
                if (AddTypeCount)
                {
                    int numArgs = type.GenericParameters.Count;
                    if (type.DeclaringType != null)
                        numArgs -= type.GenericParameters.Count;
                    if (numArgs > 0)
                    {
                        buf.Append ('`').Append (numArgs);
                    }
                }
            }
            return buf;
        }

        protected override StringBuilder AppendRefTypeName (StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            return base.AppendRefTypeName (buf, type, context).Append (RefTypeModifier);
        }

        protected override StringBuilder AppendArrayModifiers (StringBuilder buf, ArrayType array)
        {
            buf.Append (ArrayDelimeters[0]);
            int rank = array.Rank;
            if (rank > 1)
            {
                buf.Append ("0:");
                for (int i = 1; i < rank; ++i)
                {
                    buf.Append (",0:");
                }
            }
            return buf.Append (ArrayDelimeters[1]);
        }

        protected override StringBuilder AppendGenericType (StringBuilder buf, TypeReference type, IAttributeParserContext context, bool appendGeneric = true, bool useTypeProjection = false, bool isTypeofOperator = false)
        {
            if (!AddTypeCount)
                base.AppendGenericType (buf, type, context);
            else
                AppendType (buf, type, context);
            return buf;
        }

        private StringBuilder AppendType (StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            List<TypeReference> decls = DocUtils.GetDeclaringTypes (type);
            bool insertNested = false;
            int prevParamCount = 0;
            foreach (var decl in decls)
            {
                if (insertNested)
                    buf.Append (NestedTypeSeparator);
                insertNested = true;
                base.AppendTypeName (buf, decl, context);
                int argCount = DocUtils.GetGenericArgumentCount (decl);
                int numArgs = argCount - prevParamCount;
                prevParamCount = argCount;
                if (numArgs > 0)
                    buf.Append ('`').Append (numArgs);
            }
            return buf;
        }

        protected override string GetConstructorName (MethodReference constructor)
        {
            return GetMethodDefinitionName (constructor, "#ctor");
        }

        protected override string GetMethodName (MethodReference method)
        {
            string name = null;
            MethodDefinition methodDef = method as MethodDefinition;
            if (methodDef == null || !DocUtils.IsExplicitlyImplemented (methodDef))
                name = method.Name;
            else
            {
                TypeReference iface;
                MethodReference ifaceMethod;
                DocUtils.GetInfoForExplicitlyImplementedMethod (methodDef, out iface, out ifaceMethod);
                AddTypeCount = false;
                name = GetTypeName (iface) + "." + ifaceMethod.Name;
                AddTypeCount = true;
            }
            return GetMethodDefinitionName (method, name);
        }

        private string GetMethodDefinitionName (MethodReference method, string name)
        {
            StringBuilder buf = new StringBuilder ();
            buf.Append (GetTypeName (method.DeclaringType));
            buf.Append ('.');
            buf.Append (name.Replace (".", "#"));
            if (method.IsGenericMethod ())
            {
                IList<GenericParameter> genArgs = method.GenericParameters;
                if (genArgs.Count > 0)
                    buf.Append ("``").Append (genArgs.Count);
            }
            IList<ParameterDefinition> parameters = method.Parameters;
            try
            {
                genDeclType = method.DeclaringType;
                genDeclMethod = method;
                AppendParameters (buf, method.DeclaringType.GenericParameters, parameters);
            }
            finally
            {
                genDeclType = null;
                genDeclMethod = null;
            }
            return buf.ToString ();
        }

        private StringBuilder AppendParameters (StringBuilder buf, IList<GenericParameter> genArgs, IList<ParameterDefinition> parameters)
        {
            if (parameters.Count == 0)
                return buf;

            buf.Append ('(');

            AppendParameter (buf, genArgs, parameters[0]);
            for (int i = 1; i < parameters.Count; ++i)
            {
                buf.Append (',');
                AppendParameter (buf, genArgs, parameters[i]);
            }

            return buf.Append (')');
        }

        private StringBuilder AppendParameter (StringBuilder buf, IList<GenericParameter> genArgs, ParameterDefinition parameter)
        {
            AddTypeCount = false;
            buf.Append (GetTypeName (parameter.ParameterType));
            AddTypeCount = true;
            return buf;
        }

        protected override string GetPropertyName (PropertyReference property)
        {
            string name = EiiPropertyProcessing(property);

            StringBuilder buf = new StringBuilder ();
            buf.Append (GetName (property.DeclaringType));
            buf.Append ('.');
            buf.Append (name);
            IList<ParameterDefinition> parameters = property.Parameters;
            if (parameters.Count > 0)
            {
                genDeclType = property.DeclaringType;
                buf.Append ('(');
                IList<GenericParameter> genArgs = property.DeclaringType.GenericParameters;
                AppendParameter (buf, genArgs, parameters[0]);
                for (int i = 1; i < parameters.Count; ++i)
                {
                    buf.Append (',');
                    AppendParameter (buf, genArgs, parameters[i]);
                }
                buf.Append (')');
                genDeclType = null;
            }
            return buf.ToString ();
        }

        /// <summary>
        /// Vb, Cpp and F# compilers uses simple logic to generate docIds for Eii properties. No need to have special logic here.
        /// </summary>
        /// <param name="property">Value to process</param>
        /// <returns>DocId for property member wchich needs to be equal to docId generated by compiler's /doc flag </returns>
        protected virtual string EiiPropertyProcessing(PropertyReference property)
        {
            return property.Name;
        }

        protected override string GetFieldName (FieldReference field)
        {
            return string.Format ("{0}.{1}",
                GetName (field.DeclaringType), field.Name);
        }

        protected override string GetEventName (EventReference e)
        {
            return string.Format ("{0}.{1}",
                GetName (e.DeclaringType), e.Name);
        }

        protected override string GetTypeDeclaration (TypeDefinition type)
        {
            string name = GetName (type);
            if (type == null)
                return null;
            return "T:" + name;
        }

        protected override string GetConstructorDeclaration (MethodDefinition constructor)
        {
            string name = GetName (constructor);
            if (name == null)
                return null;
            return "M:" + name;
        }

        protected override string GetMethodDeclaration (MethodDefinition method)
        {
            string name = GetName (method);
            if (name == null)
                return null;
            if (method.Name == "op_Implicit" || method.Name == "op_Explicit")
            {
                genDeclType = method.DeclaringType;
                genDeclMethod = method;
                name += "~" + GetName (method.ReturnType);
                genDeclType = null;
                genDeclMethod = null;
            }
            return "M:" + name;
        }

        protected override string GetPropertyDeclaration (PropertyDefinition property)
        {
            string name = GetName (property);
            if (name == null)
                return null;
            return "P:" + name;
        }

        protected override string GetFieldDeclaration (FieldDefinition field)
        {
            string name = GetName (field);
            if (name == null)
                return null;
            return "F:" + name;
        }

        protected override string GetEventDeclaration (EventDefinition e)
        {
            string name = GetName (e);
            if (name == null)
                return null;
            return "E:" + name;
        }

        protected override string GetAttachedEventDeclaration(AttachedEventDefinition e)
        {
            string name = GetName(e);
            if (name == null)
                return null;
            return "E:" + name;
        }

        protected override string GetAttachedPropertyDeclaration(AttachedPropertyDefinition a)
        {
            string name = GetName(a);
            if (name == null)
                return null;
            return "P:" + name;
        }
    }
}