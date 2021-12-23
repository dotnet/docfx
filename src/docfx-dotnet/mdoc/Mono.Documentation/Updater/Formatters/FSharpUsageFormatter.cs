using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Mono.Documentation.Updater
{
    public class FSharpUsageFormatter : FSharpFormatter
    {
        private static readonly Dictionary<string, string> operatorsUsage = new Dictionary<string, string>()
        {
            {"~-", "-"},
            {"~+", "+"},
        };

        public FSharpUsageFormatter() : this(null) {}
        public FSharpUsageFormatter(TypeMap map) : base(map) { }

        protected override string GetMethodDeclaration(MethodDefinition method)
        {
            var buf = new StringBuilder();
            var operatorBuf = new StringBuilder();
            if (TryAppendOperatorName(operatorBuf, method))
            {
                return GetOperatorUsage(buf, method, operatorBuf.ToString());
            }

            if (method.IsStatic)
            {
                buf.Append($"{GetName(method.DeclaringType)}.{method.Name} ");
            }
            else
            {
                var typeName = AppendTypeName(new StringBuilder(), method.DeclaringType, EmptyAttributeParserContext.Empty()).ToString();
                buf.Append($"{CamelCase(typeName)}.{method.Name} ");
            }

            AppendParameters(buf, method);
            return buf.ToString();
        }

        private void AppendParameters(StringBuilder buf, MethodDefinition method)
        {
            var parameters = new List<string>();
            var curryBorders = GetCurryBorders(method);
            bool isExtensionMethod = IsExtensionMethod(method);
            for (var i = 0; i < method.Parameters.Count; i++)
            {
                if (isExtensionMethod && i == 0)
                    continue;
                if (curryBorders.Contains(i) && parameters.Count > 0)
                {
                    AppendTupleUsage(buf, parameters);
                    parameters.Clear();
                    buf.Append(" ");
                }
                var parameterDefinition = method.Parameters[i];
                parameters.Add(GetParameterUsage(parameterDefinition));
            }
            AppendTupleUsage(buf, parameters);
        }

        private string GetOperatorUsage(StringBuilder buf, MethodDefinition method, string operatorName)
        {
            var operatorMembers = operatorName.Split(' ').Skip(1).ToList();
            operatorMembers = operatorMembers.Take(operatorMembers.Count - 1).ToList();// Remove '(' and ')'
            for (var i = 0; i < operatorMembers.Count; i++)
            {
                if (operatorsUsage.ContainsKey(operatorMembers[i]))
                {
                    operatorMembers[i] = operatorsUsage[operatorMembers[i]];
                }
            }

            var curryBorders = GetCurryBorders(method);
            var parameters = new List<string>();

            if (curryBorders.Count > 0)
            {// if parameters are curried
                int operatorIndex = 0;
                for (var i = 0; i < method.Parameters.Count; i++)
                {
                    if (curryBorders.Contains(i))
                    {
                        AppendTupleUsage(buf, parameters);
                        parameters.Clear();
                        buf.Append(" ");
                        if (operatorIndex < operatorMembers.Count)
                        {
                            buf.Append(operatorMembers[operatorIndex]);
                            ++operatorIndex;
                        }
                        buf.Append(" ");
                    }
                    var parameterDefinition = method.Parameters[i];
                    parameters.Add(GetParameterUsage(parameterDefinition));
                }
                AppendTupleUsage(buf, parameters);
            }
            else
            {
                var members = new List<string>();
                for (var i = 0; i < method.Parameters.Count; i++)
                {
                    if (method.Parameters.Count <= operatorMembers.Count)
                    {
                        if (i < operatorMembers.Count)
                        {
                            members.Add(operatorMembers[i]);
                        }
                    }

                    var parameterDefinition = method.Parameters[i];
                    members.Add(GetParameterUsage(parameterDefinition));

                    if (method.Parameters.Count > operatorMembers.Count)
                    {
                        if (i < operatorMembers.Count)
                        {
                            members.Add(operatorMembers[i]);
                        }
                    }
                }
                buf.Append(string.Join(" ", members));
            }
            return buf.ToString();
        }

        private void AppendTupleUsage(StringBuilder buf, List<string> parameters)
        {
            if (parameters.Count == 0)
                return;

            if (parameters.Count == 1)
            {
                buf.Append(parameters[0]);
                return;
            }
            buf.Append($"({string.Join(", ", parameters)})");
        }

        private string GetParameterUsage(ParameterDefinition parameterDefinition)
        {
            return parameterDefinition.Name;
        }

        protected override string GetPropertyDeclaration(PropertyDefinition property)
        {
            if (DocUtils.IsExplicitlyImplemented(property))
                return DocUtils.GetPropertyName(property, NestedTypeSeparator);
            return $"{GetName(property.DeclaringType)}.{property.Name}";
        }

        protected override string GetConstructorDeclaration(MethodDefinition constructor)
        {

            StringBuilder buf = new StringBuilder();
            if (constructor.Parameters.Count == 0)
                return null;
            if (AppendVisibility(buf, constructor) == null)
                return null;

            buf.Append("new ");
            buf.Append(GetTypeName(constructor.DeclaringType));
            buf.Append(" ");
            AppendParameters(buf, constructor);
            return buf.ToString();
        }

        protected override string GetFieldDeclaration(FieldDefinition field)
        {
            return $"{GetName(field.DeclaringType)}.{field.Name}";
        }

        public override bool IsSupported(TypeReference tref) => false;
    }
}