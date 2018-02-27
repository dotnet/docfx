// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public abstract class AbstractProject
    {
        public abstract string FilePath { get; }
        public abstract bool HasDocuments { get; }
        public abstract IEnumerable<AbstractDocument> Documents { get; }
        public abstract IEnumerable<string> PortableExecutableMetadataReferences { get; }
        public abstract IEnumerable<AbstractProject> ProjectReferences { get; }
        public abstract Task<AbstractCompilation> GetCompilationAsync();
    }
}
