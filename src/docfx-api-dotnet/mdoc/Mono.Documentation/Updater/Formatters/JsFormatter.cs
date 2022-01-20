using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using Mono.Documentation.Util;

namespace Mono.Documentation.Updater.Formatters
{
    public class JsFormatter : MemberFormatter
    {

        public JsFormatter(TypeMap map) : base(map) { }

        // For the V1 Pri1 implementation, we will not implement custom “retrievers”. 
        // If a non-static class doesn’t have a public constructor 
        // (in other words, it is not possible to automatically determine the call to instantiate an instance of the class), 
        // the Javascript syntax should either:
        // 
        // show a standard disclaimer such as:
        // “This class does not provide a public constructor” or
        // “See the remarks section for information on obtaining an instance of this class”
        // Give a degenerate declarative syntax, such as simply: “Windows.System.FolderLauncherOptions” for the FolderLauncherOptions class.
        // The specific solution to use here is still TBD. If you’re blocked, pick A1 for now.
        // We will investigate whether a Pri 2 feature to modify the syntax block with custom syntax is necessary.
        public override bool IsSupported(TypeReference tref)
        {
            var type = tref.Resolve();

            if (type == null
                || type.IsAbstract
                || type.IsInterface// Interfaces: You cannot implement a Windows Runtime interface in JavaScript.
                || type.HasGenericParameters
                || !IsSupported(type.CustomAttributes)
                || type.DeclaringType != null)//WinRT type can not be nested
            {
                return false;
            }

            if (type.IsEnum ||
                type.IsValueType ||
                DocUtils.IsDelegate(type))
            {
                if (type.IsEnum && !IsEnumSupported(type)) return false;

                return true;
            }
            
            // Windows Runtime types cannot have multiple constructors with same number of arguments
            var publicConstructors = type.GetConstructors().Where(i => i.IsPublic).ToList();
            if (!publicConstructors.Any())
                return false;

            var constructorsWithEqualNumberOfArguments = publicConstructors.GroupBy(x => x.Parameters.Count)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .ToList();

            return constructorsWithEqualNumberOfArguments.Count == 0;
        }

        protected virtual bool IsEnumSupported(TypeDefinition type)
        {
            return type.GetMembers().Skip(1).Any();//skip "__value element"
        }

        public override bool IsSupported(MemberReference mref)
        {
            if (mref is PropertyDefinition)
            {
                PropertyDefinition propertyDefinition = (PropertyDefinition)mref;
                if (!IsPropertySupported(propertyDefinition))
                    return false;
            }
            else if (mref is MethodDefinition)
            {
                MethodDefinition methodDefinition = (MethodDefinition)mref;
                if (!IsMethodSupported(methodDefinition))
                    return false;
            }
            else if (mref is FieldDefinition // In WinRT fields can be exposed only by structures.
                || mref is AttachedEventDefinition
                || mref is AttachedPropertyDefinition)
                return false;

            var member = mref.Resolve();
            return member != null
                   && !member.DeclaringType.HasGenericParameters
                   && !(mref is IGenericParameterProvider genericParameterProvider && genericParameterProvider.HasGenericParameters)
                   && !(mref is IMethodSignature methodSignature && methodSignature.Parameters.Any(i => i.ParameterType is GenericParameter))
                   && mref.DeclaringType.DeclaringType == null//WinRT type can not be nested
                   && IsSupported(member.CustomAttributes);
        }

        private bool IsMethodSupported(MethodDefinition methodDefinition)
        {
            bool isDestructor = DocUtils.IsDestructor(methodDefinition);
            return
                !DocUtils.IsOperator(methodDefinition)
                && (!isDestructor || methodDefinition.DeclaringType.Interfaces.Any(i => i.InterfaceType.FullName == "Windows.Foundation.IClosable"))
                && methodDefinition.Parameters.All(i => IsSupported(i.CustomAttributes) && !(i.ParameterType is ByReferenceType))
                && IsSupported(methodDefinition.MethodReturnType.CustomAttributes);
        }

        // How to determine if an API supports JavaScript
        // Use the WebHostHidden attribute. If WebHostHidden is present, the API doesn’t support JavaScript.
        // None of the APIs in the “XAML” namespaces support JavaScript.
        protected  bool IsSupported(Collection<CustomAttribute> memberCustomAttributes)
        {
            return
                memberCustomAttributes.All(
                    i => i.AttributeType.FullName != "Windows.Foundation.Metadata.WebHostHiddenAttribute");
        }

        protected virtual bool IsPropertySupported(PropertyDefinition property)
        {
            bool getVisible = property.GetMethod != null && property.GetMethod.IsPublic;
            bool setVisible = property.SetMethod != null && property.SetMethod.IsPublic;
            if (!setVisible && !getVisible)
                return false;

            IEnumerable<MemberReference> defs = property.DeclaringType.GetDefaultMembers();
            foreach (MemberReference mi in defs)
            {
                if (mi == property)
                {
                    return false;
                }
            }
            return property.Parameters.Count == 0;
        }

        protected override StringBuilder AppendParameters(StringBuilder buf, MethodDefinition method, IList<ParameterDefinition> parameters)
        {
            return buf.Append(string.Join(", ", parameters.Select(i => i.Name)));
        }

        protected MethodDefinition GetConstructor(TypeDefinition type)
        {
            return type.GetConstructors()
                .Where(i => i.IsPublic)
                .OrderByDescending(i => i.Parameters.Count)
                .First();
        }

        protected override string GetMethodName(MethodReference method)
        {
            if (DocUtils.IsDestructor(method.Resolve()))
                return "Close";
            return CamelCase(method.Name);
        }

        protected override string GetTypeName(TypeReference type, IAttributeParserContext context, bool appendGeneric = true, bool useTypeProjection = false, bool isTypeofOperator = false)
        {
            int n = type.Name.IndexOf("`");
            if (n >= 0)
                return type.Name.Substring(0, n);
            return type.Name;
        }

        protected string ProcessFullName(string fullName)
        {
            int n = fullName.IndexOf("`");
            if (n >= 0)
                return fullName.Substring(0, n);
            return fullName;
        }
    }
}
