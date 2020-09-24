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

        public bool IsEnable(FilePath filePath, CustomRule customRule)
        {
            var canonicalVersion = _publishUrlMap.GetCanonicalVersion(filePath);
            var isCanonicalVersion = _monikerProvider.GetFileLevelMonikers(_errorLog, filePath).IsCanonicalVersion(canonicalVersion);
            if (customRule.CanonicalVersionOnly && !isCanonicalVersion)
            {
                return false;
            }

            var pageType = _documentProvider.GetPageType(filePath);

            return customRule.ContentTypes is null || customRule.ContentTypes.Contains(pageType);
        }

        public List<string> GetFileEffectiveMonikers(FilePath filePath, CustomRule? customRule)
        {
            var monikers = _monikerProvider?.GetFileLevelMonikers(_errorLog, filePath).ToList();
            if (monikers == null || !monikers.Any())
            {
                return new List<string>(new[] { string.Empty });
            }

            if (customRule != null && customRule.CanonicalVersionOnly)
            {
                var canonicalVersion = _publishUrlMap.GetCanonicalVersion(filePath);
                if (canonicalVersion != null && monikers.Contains(canonicalVersion))
                {
                    return new List<string>(new[] { canonicalVersion });
                }
                else
                {
                    return new List<string>();
                }
            }
            else
            {
                return monikers.ToList();
            }
        }
    }
}
