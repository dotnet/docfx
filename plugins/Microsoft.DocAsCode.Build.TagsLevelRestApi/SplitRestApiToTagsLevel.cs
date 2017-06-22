// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;

    //[Export("RestApiDocumentProcessor", typeof(IDocumentBuildStep))]
    public class SplitRestApiToTagsLevel : BaseDocumentBuildStep
    {
        public override string Name => nameof(SplitRestApiToTagsLevel);

        public override int BuildOrder => 1;

        public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            throw new NotImplementedException();
        }
    }
}
