// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;

    public class RoslynSourceFileBuildController : IRoslynBuildController
    {
        private readonly Compilation _compilation;
        private readonly IAssemblySymbol _assembly;
        public RoslynSourceFileBuildController(Compilation compilation, IAssemblySymbol assembly = null)
        {
            _compilation = compilation;
            _assembly = assembly ?? compilation?.Assembly;
        }

        public MetadataItem ExtractMetadata(IInputParameters parameters)
        {
            var extractor = new RoslynIntermediateMetadataExtractor(this);
            return extractor.Extract(parameters);
        }

        public IAssemblySymbol GetAssembly(IInputParameters key)
        {
            return _assembly;
        }

        public Compilation GetCompilation(IInputParameters key)
        {
            return _compilation;
        }
    }
}
