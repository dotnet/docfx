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
    public class ApplyOverwriteDocumentForRestApiRootChild : ApplyOverwriteDocument
    {
        public override string Name => nameof(ApplyOverwriteDocumentForRestApiRootChild);

        public override int BuildOrder => 0x10;

        public static readonly List<RestApiChildItemViewModel> EmptyChildItemList = new List<RestApiChildItemViewModel>();

        public Func<FileModel, string, IHostService, IEnumerable<RestApiChildItemViewModel>> GetItemsFromOverwriteDocument =
            (((overwriteModel, uid, host) =>
            {
                if (IsChildItem(uid, host))
                {
                    return OverwriteDocumentReader.Transform<RestApiChildItemViewModel>(
                        overwriteModel,
                        uid,
                        s => (RestApiChildItemViewModel)BuildRestApiDocument.BuildItem(host, s, overwriteModel, content => content != null && content.Trim() == Constants.ContentPlaceholder));
                }

                return EmptyChildItemList;
            }));

        public Func<FileModel, string, IHostService, IEnumerable<RestApiChildItemViewModel>> GetItemsToOverwrite =
            (((articleModel, uid, host) =>
            {
                var rootItem = (RestApiRootItemViewModel)articleModel.Content;
                if (rootItem.Children.Any(c => c.Uid == uid))
                {
                    return rootItem.Children.Where(c => c.Uid == uid);
                }

                return EmptyChildItemList;
            }));

        protected override void ApplyOvewriteDocument(IHostService host, List<FileModel> od, string uid, List<FileModel> articles)
        {
            ApplyOvewriteDocument(host, od, uid, articles, GetItemsFromOverwriteDocument, GetItemsToOverwrite);
        }

        private static bool IsChildItem(string uid, IHostService host)
        {
            var articleModel = host.LookupByUid(uid).FirstOrDefault(f => f.Type == DocumentType.Article);
            if (articleModel == null)
            {
                return false;
            }

            return ((RestApiRootItemViewModel)articleModel.Content).Children.Any(c => c.Uid == uid);
        }
    }
}
