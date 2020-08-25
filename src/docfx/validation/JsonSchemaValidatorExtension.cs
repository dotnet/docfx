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

        // check by CanonicalVersionOnly & ContentTypes of customRule
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
            var mime = file.Mime;

            if (mime != null &&
                file.PageType != null &&
                customRule.ContentTypes.Length > 0 &&
                !customRule.ContentTypes.Any(file.PageType.Contains))
            {
                return false;
            }

            return true;
        }
    }
}
