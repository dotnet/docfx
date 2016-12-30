// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.IO;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    [Export("ManagedReferenceDocumentProcessor", typeof(IDocumentBuildStep))]
    public class SplitClassPageIntoMethodPages : BaseDocumentBuildStep
    {
        public override string Name => nameof(SplitClassPageIntoMethodPages);

        public override int BuildOrder => 1;

        /// <summary>
        /// Extract: group with overload
        /// </summary>
        /// <param name="models"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            // TODO: split class page into method pages
            return models;
        }
    }
}
