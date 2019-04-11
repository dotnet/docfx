// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf.CoverPage
{
    using System.Collections.Generic;
    using System.Composition;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.ConceptualDocuments;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(CoverPageProcessor), typeof(IDocumentBuildStep))]
    public class BuildCoverPage : BaseDocumentBuildStep, ISupportIncrementalBuildStep
    {
        private const string ConceptualKey = DataContracts.Common.Constants.PropertyName.Conceptual;
        private const string DocumentTypeKey = "documentType";

        public override string Name => nameof(BuildCoverPage);

        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article)
            {
                return;
            }
            var content = (Dictionary<string, object>)model.Content;
            var markdown = (string)content[ConceptualKey];
            var result = host.Markup(markdown, model.OriginalFileAndType, false, true);

            var htmlInfo = HtmlDocumentUtility.SeparateHtml(result.Html);
            model.Properties.IsUserDefinedTitle = false;
            content[DataContracts.Common.Constants.PropertyName.Title] = htmlInfo.Title;
            content["rawTitle"] = htmlInfo.RawTitle;
            if (!string.IsNullOrEmpty(htmlInfo.RawTitle))
            {
                model.ManifestProperties.rawTitle = htmlInfo.RawTitle;
            }
            content[ConceptualKey] = htmlInfo.Content;
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
