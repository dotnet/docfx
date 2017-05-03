// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System.Composition;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(UniversalReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildUniversalReferenceDocument : BuildReferenceDocumentBase
    {
        private readonly IModelAttributeHandler _handler =
            new CompositeModelAttributeHandler(
                new UniqueIdentityReferenceHandler(),
                new MarkdownContentHandler()
            );

        public override string Name => nameof(BuildUniversalReferenceDocument);

        #region BuildReferenceDocumentBase

        protected override void BuildArticle(IHostService host, FileModel model)
        {
            var pageViewModel = (PageViewModel)model.Content;

            var context = new HandleModelAttributesContext
            {
                EnableContentPlaceholder = false,
                Host = host,
                FileAndType = model.OriginalFileAndType,
                SkipMarkup = pageViewModel?.ShouldSkipMarkup ?? false,
            };

            HandleAttributes<PageViewModel>(model, _handler, context);

            foreach (var reference in pageViewModel.References)
            {
                if (reference.IsExternal == false)
                {
                    host.ReportDependencyTo(model, reference.Uid, DependencyItemSourceType.Uid, DependencyTypeName.Reference);
                }
            }
        }

        #endregion
    }
}
