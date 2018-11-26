// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.CodeAnalysis;

    internal static class CodeAnalysisSymbolExtensions
    {
        /// <summary>
        /// return a symbol in the assigned compilation
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="compilation">assigned compilation</param>
        /// <param name="symbol">original symbol</param>
        /// <returns>related symbol in the compilation</returns>
        public static T FindSymbol<T>(this Compilation compilation, T symbol) where T : ISymbol
        {
            return (T)(compilation).GlobalNamespace.FindSymbol(symbol);
        }

        /// <summary>
        /// return a symbol in the assigned container
        /// </summary>
        /// <param name="container">container</param>
        /// <param name="symbol">symbol</param>
        /// <returns>related symbol in the compilation</returns>
        public static ISymbol FindSymbol(this INamespaceOrTypeSymbol container, ISymbol symbol)
        {
            return FindCore(container, GetQualifiedNameList(symbol)).Where(m => VisitorHelper.GetCommentId(m) == VisitorHelper.GetCommentId(symbol)).FirstOrDefault();
        }

        private static IEnumerable<ISymbol> FindCore(INamespaceOrTypeSymbol container, List<string> parts)
        {
            var stack = new Stack<Tuple<ISymbol, int>>();
            stack.Push(Tuple.Create<ISymbol, int>(container, 0));
            while (stack.Count > 0)
            {
                var pair = stack.Pop();
                var parent = pair.Item1;
                int index = pair.Item2;
                if (index == parts.Count)
                {
                    yield return parent;
                }
                else if (parent is INamespaceOrTypeSymbol)
                {
                    var nestedContainers = (parent as INamespaceOrTypeSymbol).GetMembers(parts[index]);

                    foreach (var c in nestedContainers)
                    {
                        stack.Push(Tuple.Create(c, index + 1));
                    }
                }
            }
        }

        private static List<string> GetQualifiedNameList(ISymbol symbol)
        {
            var names = new List<string>();
            var current = symbol;
            while ((current as INamespaceSymbol)?.IsGlobalNamespace != true)
            {
                names.Add(current.Name);
                current = current.ContainingSymbol;
            }

            names.Reverse();
            return names;
        }
    }
}
