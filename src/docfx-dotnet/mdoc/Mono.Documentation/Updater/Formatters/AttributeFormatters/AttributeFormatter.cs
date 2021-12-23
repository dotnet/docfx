using Mono.Cecil;
using Mono.Documentation.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.Documentation.Updater.Formatters
{
    public class AttributeFormatter
    {
        private AttributeValueFormatter valueFormatter = new AttributeValueFormatter();

        public virtual string PrefixBrackets { get; } = "";
        public virtual string SurfixBrackets { get; } = "";
        public virtual string Language { get; } = "";

        public static IEnumerable<(CustomAttribute, string)> GetCustomAttributes(MemberReference mi)
        {
            List<(CustomAttribute, string)> customAttributes = new List<(CustomAttribute, string)>();
            if (mi is ICustomAttributeProvider p && p.CustomAttributes?.Count > 0)
            {
                customAttributes.AddRange(PreProcessCustomAttributes(p.CustomAttributes));
            }

            if (mi is TypeDefinition typeDefinition && typeDefinition.IsSerializable)
            {
                customAttributes.Add((null, "System.Serializable"));
            }

            if (mi is PropertyDefinition pd)
            {
                if (pd.GetMethod != null)
                {
                    customAttributes.AddRange(PreProcessCustomAttributes(pd.GetMethod.CustomAttributes, "get: "));
                }
                if (pd.SetMethod != null)
                {
                    customAttributes.AddRange(PreProcessCustomAttributes(pd.SetMethod.CustomAttributes, "set: "));
                }
            }

            if (mi is EventDefinition ed)
            {
                if (ed.AddMethod != null)
                {
                    customAttributes.AddRange(PreProcessCustomAttributes(ed.AddMethod.CustomAttributes, "add: "));
                }
                if (ed.RemoveMethod != null)
                {
                    customAttributes.AddRange(PreProcessCustomAttributes(ed.RemoveMethod.CustomAttributes, "remove: "));
                }
            }
            return customAttributes;
        }

        public static IEnumerable<(CustomAttribute, string)> PreProcessCustomAttributes(IEnumerable<CustomAttribute> customAttributes, string prefix = "")
        {
            return customAttributes?.OrderBy(ca => ca.AttributeType.FullName).Select(attr => (attr, prefix));
        }

        public virtual bool TryGetAttributeString(CustomAttribute attribute, out string rval, string prefix = null, bool withBrackets = true)
        {
            if (attribute == null)
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    rval = null;
                    return false;
                }
                rval = withBrackets ? PrefixBrackets + prefix + SurfixBrackets : prefix;
                return true;
            }

            if (IsIgnoredAttribute(attribute))
            {
                rval = null;
                return false;
            }

            TypeDefinition attrType = attribute.AttributeType as TypeDefinition;
            if (attrType != null && !DocUtils.IsPublic(attrType)
                || (FormatterManager.SlashdocFormatter.GetName(attribute.AttributeType) == null)
                || Array.IndexOf(IgnorableAttributes, attribute.AttributeType.FullName) >= 0)
            {
                rval = null;
                return false;
            }

            var fields = new List<string>();

            for (int i = 0; i < attribute.ConstructorArguments.Count; ++i)
            {
                CustomAttributeArgument argument = attribute.ConstructorArguments[i];
                fields.Add(MakeAttributesValueString(
                        argument.Value,
                        argument.Type));
            }
            var namedArgs =
                (from namedArg in attribute.Fields
                 select new { Type = namedArg.Argument.Type, Name = namedArg.Name, Value = namedArg.Argument.Value })
                .Concat(
                        (from namedArg in attribute.Properties
                         select new { Type = namedArg.Argument.Type, Name = namedArg.Name, Value = namedArg.Argument.Value }))
                .OrderBy(v => v.Name);
            foreach (var d in namedArgs)
                fields.Add(MakeNamedArgumentString(d.Name, MakeAttributesValueString(d.Value, d.Type)));

            string a2 = String.Join(", ", fields.ToArray());
            if (a2 != "") a2 = "(" + a2 + ")";

            string name = attribute.GetDeclaringType();
            if (name.EndsWith("Attribute")) name = name.Substring(0, name.Length - "Attribute".Length);
            rval = withBrackets ? PrefixBrackets + prefix + name + a2 + SurfixBrackets
                : prefix + name + a2;
            return true;
        }

        protected virtual string MakeNamedArgumentString(string name, string value)
        {
            return $"{name}={value}";
        }

        public virtual string MakeAttributesValueString(object argumentValue, TypeReference argumentType)
        {
            return valueFormatter.Format(argumentType, argumentValue);
        }

        private bool IsIgnoredAttribute(CustomAttribute customAttribute)
        {
            // An Obsolete attribute with a known string is added to all ref-like structs
            // https://github.com/dotnet/csharplang/blob/master/proposals/csharp-7.2/span-safety.md#metadata-representation-or-ref-like-structs
            return customAttribute.AttributeType.FullName == typeof(ObsoleteAttribute).FullName
                && customAttribute.HasConstructorArguments
                && customAttribute.ConstructorArguments.First().Value.ToString() == Consts.RefTypeObsoleteString;
        }

        // FIXME: get TypeReferences instead of string comparison?
        private static string[] IgnorableAttributes = {
		    // Security related attributes
		    "System.Reflection.AssemblyKeyFileAttribute",
            "System.Reflection.AssemblyDelaySignAttribute",
		    // Present in @RefType
		    "System.Runtime.InteropServices.OutAttribute",
		    // For naming the indexer to use when not using indexers
		    "System.Reflection.DefaultMemberAttribute",
		    // for decimal constants
		    "System.Runtime.CompilerServices.DecimalConstantAttribute",
		    // compiler generated code
		    Consts.CompilerGeneratedAttribute,
		    // more compiler generated code, e.g. iterator methods
		    "System.Diagnostics.DebuggerHiddenAttribute",
            "System.Runtime.CompilerServices.FixedBufferAttribute",
            "System.Runtime.CompilerServices.UnsafeValueTypeAttribute",
            "System.Runtime.CompilerServices.AsyncStateMachineAttribute",
		    // extension methods
		    "System.Runtime.CompilerServices.ExtensionAttribute",
		    // Used to differentiate 'object' from C#4 'dynamic'
		    "System.Runtime.CompilerServices.DynamicAttribute",
		    // F# compiler attribute
		    "Microsoft.FSharp.Core.CompilationMapping",
        };
    }
}
