// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;

    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ResourceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildResourceDocument : IDocumentBuildStep
    {
        public string Name => nameof(BuildResourceDocument);

        public int BuildOrder => 0;

        public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            return models;
        }

        public void Build(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article && model.Type != DocumentType.Resource)
            {
                throw new NotSupportedException();
            }
            // todo : metadata.
        }

        public IEnumerable<FileModel> Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            return models;
        }
    }
}
