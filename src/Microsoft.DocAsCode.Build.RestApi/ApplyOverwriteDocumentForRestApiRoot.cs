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
    public class ApplyOverwriteDocumentForRestApiRoot : ApplyOverwriteDocument
    {
        public override string Name => nameof(ApplyOverwriteDocumentForRestApiRoot);

        public override int BuildOrder => 0x10;

        public static readonly List<RestApiRootItemViewModel> EmptyRootItemList = new List<RestApiRootItemViewModel>();

        public Func<FileModel, string, IHostService, IEnumerable<RestApiRootItemViewModel>> GetItemsFromOverwriteDocument =
            (((overwriteModel, uid, host) =>
            {
                if (IsRootItem(uid, host))
                {
                    return OverwriteDocumentReader.Transform<RestApiRootItemViewModel>(
                        overwriteModel,
                        uid,
                        s => (RestApiRootItemViewModel)BuildRestApiDocument.BuildItem(host, s, overwriteModel, content => content != null && content.Trim() == Constants.ContentPlaceholder));
                }

                return EmptyRootItemList;
            }));

        public Func<FileModel, string, IHostService, IEnumerable<RestApiRootItemViewModel>> GetItemsToOverwrite =
            (((articleModel, uid, host) =>
            {
                var rootItem = (RestApiRootItemViewModel)articleModel.Content;
                if (uid == rootItem.Uid)
                {
                    return new[] { rootItem };
                }

                return EmptyRootItemList;
            }));

        protected override void ApplyOvewriteDocument(IHostService host, List<FileModel> od, string uid, List<FileModel> articles)
        {
            ApplyOvewriteDocument(host, od, uid, articles, GetItemsFromOverwriteDocument, GetItemsToOverwrite);
        }

        private static bool IsRootItem(string uid, IHostService host)
        {
            var articleModel = host.LookupByUid(uid).FirstOrDefault(f => f.Type == DocumentType.Article);
            if (articleModel == null)
            {
                return false;
            }

            return uid == ((RestApiRootItemViewModel)articleModel.Content).Uid;
        }
    }
}
