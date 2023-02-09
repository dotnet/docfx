// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dotnet
{
    using Microsoft.CodeAnalysis;

    internal class RoslynIntermediateMetadataExtractor
    {
        public static MetadataItem GenerateYamlMetadata(IAssemblySymbol assembly = null, ExtractMetadataOptions options = null, IMethodSymbol[] extensionMethods = null)
        {
            return assembly.Accept(new SymbolVisitorAdapter(new YamlModelGenerator(), options ?? new(), extensionMethods));
        }
    }
}
