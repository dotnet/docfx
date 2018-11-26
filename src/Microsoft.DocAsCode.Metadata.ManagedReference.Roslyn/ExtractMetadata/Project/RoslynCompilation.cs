// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;

    public class RoslynCompilation : AbstractCompilation
    {
        Compilation _compilation;

        public RoslynCompilation(Compilation compilation)
        {
            _compilation = compilation;

        }

        public Compilation Compilation => _compilation;

        public override IBuildController GetBuildController()
        {
            return new RoslynSourceFileBuildController(this.Compilation);
        }
    }
}
