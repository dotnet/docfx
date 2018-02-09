// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.CodeAnalysis;

    public class RoslynIntermediateMetadataExtractor : IExtractor
    {
        private IRoslynBuildController _controller;
        public RoslynIntermediateMetadataExtractor(IRoslynBuildController controller)
        {
            _controller = controller;
        }

        public MetadataItem Extract(IInputParameters key)
        {
            var compilation = _controller.GetCompilation(key);
            var assembly = _controller.GetAssembly(key);

            return GenerateYamlMetadata(compilation, assembly, key.Options);
        }

        internal static MetadataItem GenerateYamlMetadata(Compilation compilation, IAssemblySymbol assembly = null, ExtractMetadataOptions options = null)
        {
            if (compilation == null)
            {
                return null;
            }

            options = options ?? new ExtractMetadataOptions();

            return new RoslynMetadataExtractor(compilation, assembly).Extract(options);
        }

        public static IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> GetAllExtensionMethodsFromCompilation(IEnumerable<Compilation> compilations)
        {
            var methods = new Dictionary<Compilation, IEnumerable<IMethodSymbol>>();
            foreach (var compilation in compilations)
            {
                if (compilation.Assembly.MightContainExtensionMethods)
                {
                    var extensions = (from n in GetAllNamespaceMembers(compilation.Assembly).Distinct()
                                      from m in GetExtensionMethodPerNamespace(n)
                                      select m).ToList();
                    if (extensions.Count > 0)
                    {
                        methods[compilation] = extensions;
                    }
                }
            }
            return methods;
        }

        public static IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> GetAllExtensionMethodsFromAssembly(Compilation compilation, IEnumerable<IAssemblySymbol> assemblies)
        {
            var methods = new Dictionary<Compilation, IEnumerable<IMethodSymbol>>();
            foreach (var assembly in assemblies)
            {
                if (assembly.MightContainExtensionMethods)
                {
                    var extensions = (from n in GetAllNamespaceMembers(assembly).Distinct()
                                      from m in GetExtensionMethodPerNamespace(n)
                                      select m).ToList();
                    if (extensions.Count > 0)
                    {
                        if (methods.TryGetValue(compilation, out IEnumerable<IMethodSymbol> ext))
                        {
                            methods[compilation] = ext.Union(extensions);
                        }
                        else
                        {
                            methods[compilation] = extensions;
                        }
                    }
                }
            }
            return methods;
        }

        private static IEnumerable<INamespaceSymbol> GetAllNamespaceMembers(IAssemblySymbol assembly)
        {
            var queue = new Queue<INamespaceSymbol>();
            queue.Enqueue(assembly.GlobalNamespace);
            while (queue.Count > 0)
            {
                var space = queue.Dequeue();
                yield return space;
                var childSpaces = space.GetNamespaceMembers();
                foreach (var child in childSpaces)
                {
                    queue.Enqueue(child);
                }
            }
        }

        private static IEnumerable<IMethodSymbol> GetExtensionMethodPerNamespace(INamespaceSymbol space)
        {
            var typesWithExtensionMethods = space.GetTypeMembers().Where(t => t.MightContainExtensionMethods);
            foreach (var type in typesWithExtensionMethods)
            {
                var members = type.GetMembers();
                foreach (var member in members)
                {
                    if (member.Kind == SymbolKind.Method)
                    {
                        var method = (IMethodSymbol)member;
                        if (method.IsExtensionMethod)
                        {
                            yield return method;
                        }
                    }
                }
            }
        }
    }
}
