// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(RestApiDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteDocumentForRestApi : ApplyOverwriteDocument<RestApiItemViewModel>
    {
        public override string Name => nameof(ApplyOverwriteDocumentForRestApi);

        public override int BuildOrder => 0x10;

        protected override IEnumerable<RestApiItemViewModel> GetItemsFromOverwriteDocument(FileModel fileModel, IHostService host)
        {
            var item = OverwriteDocumentReader.Transform<RestApiItemViewModel>(
                fileModel,
                s => BuildRestApiDocument.BuildItem(host, s, fileModel, content => content != null && content.Trim() == Constants.ContentPlaceholder));
            fileModel.Content = item;
            return item;
        }

        protected override IEnumerable<RestApiItemViewModel> GetItemsToOverwrite(FileModel model, IHostService host)
        {
            return new RestApiItemViewModel[] { (RestApiItemViewModel)model.Content }.Concat(((RestApiItemViewModel)model.Content).Children);
        }
    }
}
