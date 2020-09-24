// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

        public bool IsEnable(FilePath filePath, CustomRule customRule, string? moniker = null)
        {
            var canonicalVersion = _publishUrlMap.GetCanonicalVersion(filePath);

            // If content versioning not enabled for this depot, canonicalVersion will be null; otherwise, rule should be effective only on canonical version;
            // If content versioning enabled and moniker is null, we should check file-level monikers to be sure;
            // If content versioning enabled and moniker is not null, just compare canonicalVersion and moniker.
            var isCanonicalVersion = string.IsNullOrEmpty(canonicalVersion) ? true :
                string.IsNullOrEmpty(moniker) ? _monikerProvider.GetFileLevelMonikers(_errorLog, filePath).IsCanonicalVersion(canonicalVersion) :
                canonicalVersion == moniker;

            if (customRule.CanonicalVersionOnly && !isCanonicalVersion)
            {
                return false;
            }

            var pageType = _documentProvider.GetPageType(filePath);

            return customRule.ContentTypes is null || customRule.ContentTypes.Contains(pageType);
        }
    }
}
