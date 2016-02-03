// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public interface IDocumentBuildStep
    {
        string Name { get; }
        int BuildOrder { get; }
        IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host);
        void Build(FileModel model, IHostService host);
        void Postbuild(ImmutableList<FileModel> models, IHostService host);
    }
}
