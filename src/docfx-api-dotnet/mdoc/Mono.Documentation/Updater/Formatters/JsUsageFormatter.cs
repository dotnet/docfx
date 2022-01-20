using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Documentation;
using Mono.Documentation.Updater;
using Mono.Documentation.Util;

namespace Mono.Documentation.Updater.Formatters
{
    public class JsUsageFormatter : JsFormatter
    {
        public override string Language => "JavaScript (usage)";

        public JsUsageFormatter() : this(null) {}
        public JsUsageFormatter(TypeMap map) : base(map) { }

        protected override string GetPropertyDeclaration(PropertyDefinition property)
        {
            bool getVisible = property.GetMethod != null && property.GetMethod.IsPublic;
            bool setVisible = property.SetMethod != null && property.SetMethod.IsPublic;
            var method = property.SetMethod ?? property.GetMethod;

            // https://github.com/mono/api-doc-tools/issues/133
            // var [property value type, camel-cased] = [typename, camel-cased].[property name, camel-cased];
            // [typename, camel-cased].[property name, camel-cased] = [property value type, camel-cased];
            // Static properties
            // var [property value type, camel-cased] = [typename].[property name, camel-cased];
            // [typename].[property name, camel-cased] = [property value type, camel-cased];
            var buf = new StringBuilder();
            var propertyValueType = CamelCase(GetName(property.PropertyType));
            var propertyName = DocUtils.GetPropertyName(property, NestedTypeSeparator);
            var propertyNameCamelCased = CamelCase(propertyName);
            var typeName = GetName(property.DeclaringType);
            if (!method.IsStatic)
                typeName = CamelCase(typeName);

            if (getVisible)
            {
                buf.Append("var ");
                buf.Append(propertyValueType);
                buf.Append(" = ");
                buf.Append(typeName);
                buf.Append(".");
                buf.Append(propertyNameCamelCased);
                buf.Append(";");
            }

            if (setVisible)
            {
                if (getVisible)
                    buf.Append(GetLineEnding());

                buf.Append(typeName);
                buf.Append(".");
                buf.Append(propertyNameCamelCased);
                buf.Append(" = ");
                buf.Append(propertyValueType);
                buf.Append(";");
            }
            return buf.ToString();
        }

        protected override string GetTypeDeclaration(TypeDefinition type)
        {
            var buf = new StringBuilder();

            if (type.IsEnum)
            {
                // var value = [fully qualified type].[camel-cased name of first enum value];
                var firstElement = type.GetMembers().Skip(1).First();//skip "__value element"

                buf.Append("var value = ");
                buf.Append(ProcessFullName(type.FullName));
                buf.Append(".");
                buf.Append(CamelCase(firstElement.Name));
                return buf.ToString();
            }
            if (type.IsValueType)
            {
                //Structures: Windows Runtime structures are objects in JavaScript. 
                // If you want to pass a Windows Runtime structure to a Windows Runtime method, 
                // don't instantiate the structure with the new keyword. Instead, create an object 
                // and add the relevant members and their values. The names of the members should be in camel case:
                // SomeStruct.firstMember.

                // var [struct name, camel cased] = {
                //	[fieldname, came cased] : /* Your value */
                buf.Append("var ");
                buf.Append(CamelCase(GetName(type)));
                buf.Append(" = {");
                buf.Append(GetLineEnding());
                buf.Append(string.Join("," + GetLineEnding(), 
                    type.Fields.Select(i => CamelCase(i.Name) + " : /* Your value */")));
                buf.Append(GetLineEnding());
                buf.Append("}");
                return buf.ToString();
            }
            if (DocUtils.IsDelegate(type))
            {
                //  var [delegateName, camel-cased]Handler = function([parameter name list, camel cased]){
                //  /* Your code */
                //            }
                MethodDefinition invoke = type.GetMethod("Invoke");
                buf.Append("var ");
                buf.Append(CamelCase(GetName(type)));
                buf.Append("Handler = function(");
                AppendParameters(buf, invoke, invoke.Parameters);
                buf.Append("){");
                buf.Append(GetLineEnding());
                buf.Append("/* Your code */");
                buf.Append(GetLineEnding());
                buf.Append("}");
                return buf.ToString();
            }
            
            var publicConstructor = GetConstructor(type);
            return GetDeclaration(publicConstructor);
        }

        protected override string GetMethodDeclaration(MethodDefinition method)
        {
            var buf = new StringBuilder();

            if (IsAsync(method))
            {
                // Async Methods
                // (For static methods, use the fully-qualified class name. For non-static, 
                // use the class name without qualification and camel-cased.)
                // [fully qualified type name].[camel-cased method name]([parameter names]).done( /* Your success and error handlers */ );
                var typeName = CamelCase(GetName(method.DeclaringType));
                if (method.IsStatic)
                    typeName = ProcessFullName(method.DeclaringType.FullName);

                buf.Append(typeName);
                buf.Append(".");
                buf.Append(GetMethodName(method));
                buf.Append("(");
                AppendParameters(buf, method, method.Parameters);
                buf.Append(").done( /* Your success and error handlers */ )");

                return buf.ToString();
            }

            // For static and non-static method generate different signatures:
            // 1) Non-static
            // Usage (not void)
            // var [return type, camel-cased] = [class-name, camel-cased].[method-name, camel-cased]([parameter name list]);
            // Usage (void)
            // [class-name, camel-cased].[method-name, camel cased]([parameter name list]);
            // 2) Static
            // Usage (not void)
            // var [return type, camel-cased] = [fully-qualified class-name].[method-name, camel-cased]([parameter n
            // Usage (void)
            // [fully-qualified class name].[method-name, camel cased]([parameter name list]);
            if (method.ReturnType != null && ProcessFullName(method.ReturnType.FullName) != Consts.VoidFullName)
            {
                buf.Append("var ");
                buf.Append(CamelCase(GetName(method.ReturnType)));
                buf.Append(" = ");
            }
            var className = method.IsStatic ? ProcessFullName(method.DeclaringType.FullName) : CamelCase(GetName(method.DeclaringType));
            buf.Append(className);
            buf.Append(".");
            buf.Append(CamelCase(method.Name));
            buf.Append("(");
            AppendParameters(buf, method, method.Parameters);
            buf.Append(")");

            return buf.ToString();
        }

        protected override string GetConstructorDeclaration(MethodDefinition constructor)
        {
            var buf = new StringBuilder();

            var typeName = GetName(constructor.DeclaringType);
            buf.Append("var ");
            buf.Append(CamelCase(typeName));
            buf.Append(" = new ");
            buf.Append(typeName);
            buf.Append("(");
            AppendParameters(buf, constructor, constructor.Parameters);
            buf.Append(");");

            return buf.ToString();
        }

        private bool IsAsync(MethodDefinition method)
        {
            return method.CustomAttributes.Any(i => i.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute");
        }

        protected override StringBuilder AppendNamespace(StringBuilder buf, TypeReference type)
        {
            return buf;
        }

        protected override string GetEventDeclaration(EventDefinition e)
        {
            // Usage:
            //function on[EventName](eventArgs){/* Your code */}
            //[class name, camel cased].addEventListener(“[event name, all lower-case]”, on[EventName]); 
            //[class name, camel cased].removeEventListener(“[event name, all lower-case]”, on[EventName]);
            // If the event supports property syntax (it has a corresponding EventName property), add this:
            // - or -
            //[class name, camel cased].on[event name, all lower-case] = on[EventName];
            if (!e.AddMethod.IsPublic)
                return null;

            var className = e.AddMethod.IsStatic ? ProcessFullName(e.DeclaringType.FullName) : CamelCase(GetName(e.DeclaringType));
            var eventName = e.Name;
            var eventNameLowerCased = eventName.ToLower();
            var buf = new StringBuilder();
            buf.Append("function on");
            buf.Append(e.Name);
            buf.Append("(");
            buf.Append("eventArgs");
            buf.Append(") { /* Your code */ }");

            buf.Append(GetLineEnding());
            buf.Append(className);
            buf.Append(".addEventListener(\"");
            buf.Append(eventNameLowerCased);
            buf.Append("\", on");
            buf.Append(eventName);
            buf.Append(");");

            buf.Append(GetLineEnding());
            buf.Append(className);
            buf.Append(".removeEventListener(\"");
            buf.Append(eventNameLowerCased);
            buf.Append("\", on");
            buf.Append(eventName);
            buf.Append(");");

            buf.Append(GetLineEnding());
            buf.Append("- or -");
            buf.Append(GetLineEnding());
            buf.Append(className);
            buf.Append(".on");
            buf.Append(eventNameLowerCased);
            buf.Append(" = on");
            buf.Append(eventName);
            buf.Append(";");

            return buf.ToString();
        }
    }
}
