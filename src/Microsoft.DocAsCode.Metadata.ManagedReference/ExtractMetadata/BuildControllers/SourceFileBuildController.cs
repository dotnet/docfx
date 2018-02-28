// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;

    internal class SourceFileBuildController : IBuildController
    {
        private readonly Compilation _compilation;
        private readonly IAssemblySymbol _assembly;
        public SourceFileBuildController(Compilation compilation, IAssemblySymbol assembly = null)
        {
            _compilation = compilation;
            _assembly = assembly ?? _compilation?.Assembly;
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
