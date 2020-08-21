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

        internal bool IsEnable(FilePath filePath, CustomRule customRule)
        {
            var siteUrl = _documentProvider.GetDocsSiteUrl(filePath);
            var canonicalVersion = _publishUrlMap.GetCanonicalVersion(siteUrl);
            var isCanonicalVersion = _monikerProvider.GetFileLevelMonikers(_errorLog, filePath).IsCanonicalVersion(canonicalVersion);
            if (customRule.CanonicalVersionOnly && (!isCanonicalVersion ?? false))
            {
                return false;
            }

            var file = _documentProvider.GetDocument(filePath);
            var mime = file?.Mime;

            if (mime != null
                && file != null
                && file.PageType != null
                && customRule.ContentTypes.Length > 0
                && !customRule.ContentTypes.Any(file.PageType.Contains))
            {
                return false;
            }

            return true;
        }

        internal Error? GetError(Error error, JsonSchema schema, FilePath? filePath)
        {
            if (!string.IsNullOrEmpty(error.Name) &&
                schema.Rules.TryGetValue(error.Name, out var attributeCustomRules) &&
                attributeCustomRules.TryGetValue(error.Code, out var customRule))
            {
                if (filePath == null)
                {
                    return error.WithCustomRule(customRule, null);
                }
                else
                {
                    return error.WithCustomRule(customRule, IsEnable(filePath, customRule));
                }
            }

            return null;
        }
    }
}
