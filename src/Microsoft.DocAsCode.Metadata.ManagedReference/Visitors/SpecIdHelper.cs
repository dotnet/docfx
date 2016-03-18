// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    internal sealed class SpecIdHelper
    {
        private static readonly Regex TypeParameterRegex = new Regex(@"\B(?<!`)`\d+", RegexOptions.Compiled);
        private static readonly Regex MethodParameterRegex = new Regex(@"\B``\d+", RegexOptions.Compiled);

        public static string GetSpecId(
            ISymbol symbol,
            IReadOnlyList<string> typeGenericParameters,
            IReadOnlyList<string> methodGenericParameters = null)
        {
            var id = symbol.Accept(SpecIdCoreVisitor.Instance);
            if (methodGenericParameters == null)
            {
                id = SpecMethodGenericParameter(symbol as IMethodSymbol ?? symbol.ContainingSymbol as IMethodSymbol, id);
            }
            else
            {
                id = SpecMethodGenericParameter(methodGenericParameters, id);
            }
            id = SpecTypeGenericParameter(typeGenericParameters, id);
            return id;
        }

        private static string SpecMethodGenericParameter(IMethodSymbol symbol, string id)
        {
            if (symbol == null)
            {
                return id;
            }
            return MethodParameterRegex.Replace(
                id,
                match =>
                {
                    Debug.Assert(symbol.IsGenericMethod);
                    Debug.Assert(symbol.TypeParameters.Length > int.Parse(match.Value.Substring(2)));
                    return "{" + symbol.TypeParameters[int.Parse(match.Value.Substring(2))].Name + "}";
                });
        }

        private static string SpecTypeGenericParameter(IReadOnlyList<string> names, string id)
        {
            if (names == null || names.Count == 0)
            {
                return id;
            }
            return TypeParameterRegex.Replace(
                id,
                match =>
                {
                    Debug.Assert(names.Count > int.Parse(match.Value.Substring(1)));
                    return "{" + names[int.Parse(match.Value.Substring(1))] + "}";
                });
        }

        private static string SpecMethodGenericParameter(IReadOnlyList<string> names, string id)
        {
            if (names == null || names.Count == 0)
            {
                return id;
            }
            return MethodParameterRegex.Replace(
                id,
                match =>
                {
                    Debug.Assert(names.Count > int.Parse(match.Value.Substring(2)));
                    return "{" + names[int.Parse(match.Value.Substring(2))] + "}";
                });
        }
    }
}
