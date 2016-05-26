// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.RestApi.ViewModels;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(RestApiDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteDocumentForRestApi : ApplyOverwriteDocument
    {
        public override string Name => nameof(ApplyOverwriteDocumentForRestApi);

        public override int BuildOrder => 0x10;

        public Func<FileModel, string, IHostService, IEnumerable<RestApiItemViewModel>> GetItemsFromOverwriteDocument =
            (((fileModel, uid, host) =>
            {
                return OverwriteDocumentReader.Transform<RestApiItemViewModel>(
                    fileModel,
                    uid,
                    s => BuildRestApiDocument.BuildItem(host, s, fileModel, content => content != null && content.Trim() == Constants.ContentPlaceholder));
            }));

        public Func<FileModel, string, IHostService, IEnumerable<RestApiItemViewModel>> GetItemsToOverwrite =
            (((fileModel, uid, host) =>
            {
                return (new [] { (RestApiItemViewModel)fileModel.Content }.Concat(((RestApiItemViewModel)fileModel.Content).Children)).Where(s => s.Uid == uid);
            }));

        protected override void ApplyOvewriteDocument(IHostService host, List<FileModel> od, string uid, List<FileModel> articles)
        {
            ApplyOvewriteDocument(host, od, uid, articles, GetItemsFromOverwriteDocument, GetItemsToOverwrite);
        }
    }
}
