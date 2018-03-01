// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ResourceFiles
{
    using System.Collections.Generic;
    using System.Composition;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ResourceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ValidateResourceMetadata : BaseDocumentBuildStep, ISupportIncrementalBuildStep
    {
        public override string Name => nameof(ValidateResourceMetadata);

        public override int BuildOrder => 1;

        public override void Build(FileModel model, IHostService host)
        {
            if (!host.HasMetadataValidation)
            {
                return;
            }
            var metadata = (Dictionary<string, object>)model.Content;
            if (metadata != null)
            {
                host.ValidateInputMetadata(
                    model.OriginalFileAndType.File,
                    metadata.ToImmutableDictionary());
            }
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
