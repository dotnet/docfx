// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Composition;

    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ResourceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildResourceDocument : BaseDocumentBuildStep
    {
        public override string Name => nameof(BuildResourceDocument);

        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article && model.Type != DocumentType.Resource)
            {
                throw new NotSupportedException();
            }
            // todo : metadata.
        }
    }
}
