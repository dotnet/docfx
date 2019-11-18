// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ConceptualDocuments
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ConceptualDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ValidateConceptualDocumentMetadata : BaseDocumentBuildStep, ISupportIncrementalBuildStep
    {
        private const string ConceptualKey = Constants.PropertyName.Conceptual;

        public override string Name => nameof(ValidateConceptualDocumentMetadata);

        public override int BuildOrder => 1;

        public override void Build(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article)
            {
                return;
            }
            if (!host.HasMetadataValidation)
            {
                return;
            }
            var metadata = ((Dictionary<string, object>)model.Content).ToImmutableDictionary().Remove(ConceptualKey);
            if(!model.Properties.IsUserDefinedTitle)
            {
                metadata = metadata.Remove(Constants.PropertyName.Title);
            }
            host.ValidateInputMetadata(model.OriginalFileAndType.File, metadata);
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
