// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaValidatorExtension
    {
        private readonly BuildScope _buildScope;
        private readonly DocumentProvider _documentProvider;
        private readonly PublishUrlMap _publishUrlMap;
        private readonly MonikerProvider _monikerProvider;
        private readonly ErrorBuilder _errorLog;

        // mime -> page type. TODO get from docs-ui schema
        private readonly Dictionary<string, string> _mapping = new Dictionary<string, string>
        {
            { "NetType", "dotnet" },
            { "NetNamespace", "dotnet" },
            { "NetMember", "dotnet" },
            { "NetEnum", "dotnet" },
            { "NetDelegate ", "dotnet" },
            { "RESTOperation", "rest" },
            { "RESTOperationGroup ", "rest" },
            { "RESTService  ", "rest" },
            { "PowershellCmdlet", "powershell" },
            { "PowershellModule ", "powershell" },
        };

        public JsonSchemaValidatorExtension(
            BuildScope buildScope,
            DocumentProvider documentProvider,
            PublishUrlMap publishUrlMap,
            MonikerProvider monikerProvider,
            ErrorBuilder errorLog)
        {
            _buildScope = buildScope;
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

            var contentType = _buildScope.GetContentType(filePath);
            var mime = _buildScope.GetMime(contentType, filePath);

            if (mime != null
                && _mapping.TryGetValue(mime!, out var pageType)
                && customRule.ContentTypes.Length > 0
                && !customRule.ContentTypes.Any(pageType.Contains))
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

        internal bool IsInValidateScope(JObject metadata, FilePath filePath)
        {
            var contentType = _buildScope.GetContentType(filePath);
            var mime = _buildScope.GetMime(contentType, filePath);

            // Only validate conceptual and reference files
            return contentType == ContentType.Page && (IsConceptual(mime, metadata) || IsReference(mime));
        }

        private static bool IsConceptual(string? mime, JObject metadata)
        {
            return mime == "Conceptual" &&
                    (!metadata.ContainsKey("layout") ||
                     string.Equals(metadata.GetValue("layout")?.ToString(), "conceptual", StringComparison.OrdinalIgnoreCase));
        }

        private bool IsReference(string? mime)
        {
            return mime != null && _mapping.ContainsKey(mime);
        }
    }
}
