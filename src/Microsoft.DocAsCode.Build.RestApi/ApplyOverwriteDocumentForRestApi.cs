// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.RestApi.ViewModels;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(RestApiDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteDocumentForRestApi : ApplyOverwriteDocument<RestApiItemViewModel>
    {
        public override string Name => nameof(ApplyOverwriteDocumentForRestApi);

        public override int BuildOrder => 0x10;

        protected override IEnumerable<RestApiItemViewModel> GetItemsFromOverwriteDocument(FileModel fileModel, string uid, IHostService host)
        {
            return OverwriteDocumentReader.Transform<RestApiItemViewModel>(
                fileModel,
                uid,
                s => BuildRestApiDocument.BuildItem(host, s, fileModel, content => content != null && content.Trim() == Constants.ContentPlaceholder));
        }

        protected override IEnumerable<RestApiItemViewModel> GetItemsToOverwrite(FileModel model, string uid, IHostService host)
        {
            return (new RestApiItemViewModel[] { (RestApiItemViewModel)model.Content }.Concat(((RestApiItemViewModel)model.Content).Children)).Where(s => s.Uid == uid);
        }
    }
}
