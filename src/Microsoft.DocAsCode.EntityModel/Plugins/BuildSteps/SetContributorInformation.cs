// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System.Composition;

    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ConceptualDocumentProcessor), typeof(IDocumentBuildStep))]
    public class SetContributorInformation : BaseDocumentBuildStep
    {
        public override string Name => nameof(SetContributorInformation);

        public override int BuildOrder => 2;

        public override void Build(FileModel model, IHostService host)
        {
            // TODO: set contributor information in metadata
        }
    }
}
