// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using YamlDotNet.Serialization;

    [Serializable]
    public class AttributeFilterInfo
    {
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "ctorArguments")]
        public List<string> ConstructorArguments { get; set; } = new List<string>();

        [YamlMember(Alias = "ctorNamedArguments")]
        public Dictionary<string, string> ConstructorNamedArguments { get; set; } = new Dictionary<string, string>();

        public bool ContainedIn(ISymbol symbol)
        {
            bool result = false;
            var attributes = symbol.GetAttributes();
            foreach (var attribute in attributes)
            {
                INamedTypeSymbol attributeClass = attribute.AttributeClass;
                if (Uid != VisitorHelper.GetId(attributeClass))
                {
                    continue;
                }

                // arguments need to be a total match of the config
                IEnumerable<string> arguments = attribute.ConstructorArguments.Select(arg => GetLiteralString(arg));
                if (!ConstructorArguments.SequenceEqual(arguments))
                {
                    continue;
                }

                // namedarguments need to be a superset of the config
                Dictionary<string, string> namedArguments = attribute.NamedArguments.ToDictionary(pair => pair.Key, pair => GetLiteralString(pair.Value));
                if (!ConstructorNamedArguments.Except(namedArguments).Any())
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        private static string GetLiteralString(TypedConstant constant)
        {
            var type = constant.Type;
            var value = constant.Value;

            if (type.TypeKind == TypeKind.Enum)
            {
                var namedType = (INamedTypeSymbol)type;
                var pairs = (from member in namedType.GetMembers().OfType<IFieldSymbol>()
                             where member.IsConst && member.HasConstantValue
                             select Tuple.Create(member.Name, member.ConstantValue)).ToDictionary(tuple => tuple.Item2, tuple => tuple.Item1);

                return $"{VisitorHelper.GetId(namedType)}.{pairs[value]}";
            }

            if (value is ITypeSymbol)
            {
                return VisitorHelper.GetId((ITypeSymbol)value);
            }

            return value.ToString();
        }
    }
}
