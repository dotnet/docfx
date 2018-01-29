// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;

    public interface IRoslynBuildController : IBuildController
    {
        Compilation GetCompilation(IInputParameters key);
        IAssemblySymbol GetAssembly(IInputParameters key);
    }

}
