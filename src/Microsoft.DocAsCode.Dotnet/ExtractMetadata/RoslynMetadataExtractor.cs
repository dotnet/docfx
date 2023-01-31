// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Diagnostics;

    using Microsoft.CodeAnalysis;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    internal class RoslynMetadataExtractor
    {
        private readonly Compilation _compilation;
        private readonly IAssemblySymbol _assembly;

        public RoslynMetadataExtractor(Compilation compilation, IAssemblySymbol assembly = null)
        {
            _compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            _assembly = assembly ?? compilation.Assembly;
        }

        public MetadataItem Extract(ExtractMetadataOptions options)
        {
            var preserveRawInlineComments = options.PreserveRawInlineComments;
            var filterConfigFile = options.FilterConfigFile;
            var extensionMethods = options.RoslynExtensionMethods;

            object visitorContext = new object();
            SymbolVisitorAdapter visitor;
            if (_compilation.Language == "Visual Basic")
            {
                visitor = new SymbolVisitorAdapter(new CSYamlModelGenerator() + new VBYamlModelGenerator(), SyntaxLanguage.VB, _compilation, options);
            }
            else if (_compilation.Language == "C#")
            {
                visitor = new SymbolVisitorAdapter(new CSYamlModelGenerator() + new VBYamlModelGenerator(), SyntaxLanguage.CSharp, _compilation, options);
            }
            else
            {
                Debug.Assert(false, "Language not supported: " + _compilation.Language);
                Logger.Log(LogLevel.Error, "Language not supported: " + _compilation.Language);
                return null;
            }

            MetadataItem item = _assembly.Accept(visitor);
            return item;
        }
    }
}
