// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;

    internal interface IBuildController
    {
        Compilation GetCompilation(IInputParameters key);
        IAssemblySymbol GetAssembly(IInputParameters key);
    }
}
