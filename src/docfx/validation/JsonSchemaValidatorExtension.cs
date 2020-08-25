// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaValidatorExtension
    {
        private readonly DocumentProvider _documentProvider;
        private readonly PublishUrlMap _publishUrlMap;
        private readonly MonikerProvider _monikerProvider;
        private readonly ErrorBuilder _errorLog;

        public JsonSchemaValidatorExtension(
            DocumentProvider documentProvider,
            PublishUrlMap publishUrlMap,
            MonikerProvider monikerProvider,
            ErrorBuilder errorLog)
        {
            _documentProvider = documentProvider;
            _publishUrlMap = publishUrlMap;
            _monikerProvider = monikerProvider;
            _errorLog = errorLog;
        }

        public bool IsEnable(FilePath filePath, CustomRule customRule)
        {
            var siteUrl = _documentProvider.GetSiteUrl(filePath);
            var canonicalVersion = _publishUrlMap.GetCanonicalVersion(siteUrl);
            var isCanonicalVersion = _monikerProvider.GetFileLevelMonikers(_errorLog, filePath).IsCanonicalVersion(canonicalVersion);
            if (customRule.CanonicalVersionOnly && !isCanonicalVersion)
            {
                return false;
            }

            var file = _documentProvider.GetDocument(filePath);
            if (file.PageType is null)
            {
                return false;
            }

            return customRule.ContentTypes is null || customRule.ContentTypes.Contains(file.PageType);
        }
    }
}
