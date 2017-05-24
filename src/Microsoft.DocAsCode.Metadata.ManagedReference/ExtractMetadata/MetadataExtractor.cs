// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using System;
    using System.Diagnostics;

    internal class MetadataExtractor
    {
        private Compilation _compilation;
        private IAssemblySymbol _assembly;
        private bool preserveRawComments;
        private string filterConfigFile;
        public MetadataExtractor(Compilation compilation, IAssemblySymbol assembly = null)
        {
            _compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            _assembly = assembly ?? compilation.Assembly;
        }

        public MetadataItem Extract(ExtractMetadataOptions options)
        {
            var preserveRawInlineComments = options.PreserveRawinlineComments;
            var filterConfigFile = options.FilterConfigFile;
            var extensionMethods = options.ExtensionMethods;

            object visitorContext = new object();
            SymbolVisitorAdapter visitor;
            if (_compilation.Language == "Visual Basic")
            {
                visitor = new SymbolVisitorAdapter(new CSYamlModelGenerator() + new VBYamlModelGenerator(), SyntaxLanguage.VB, _compilation, preserveRawInlineComments, filterConfigFile, extensionMethods);
            }
            else if (_compilation.Language == "C#")
            {
                visitor = new SymbolVisitorAdapter(new CSYamlModelGenerator() + new VBYamlModelGenerator(), SyntaxLanguage.CSharp, _compilation, preserveRawInlineComments, filterConfigFile, extensionMethods);
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
