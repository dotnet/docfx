// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class MetadataValidator
    {
        private readonly DocumentProvider _documentProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly PublishUrlMap _publishUrlMap;
        private readonly BuildScope _buildScope;
        private readonly JsonSchemaValidator[] _schemaValidators;
        private readonly HashSet<string> _reservedMetadata;

        public JsonSchema[] MetadataSchemas { get; }

        public MetadataValidator(
            Config config,
            MicrosoftGraphAccessor microsoftGraphAccessor,
            FileResolver fileResolver,
            BuildScope buildScope,
            DocumentProvider documentProvider,
            MonikerProvider monikerProvider,
            PublishUrlMap publishUrlMap)
        {
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;
            _publishUrlMap = publishUrlMap;
            _buildScope = buildScope;

            MetadataSchemas = Array.ConvertAll(
               config.MetadataSchema,
               schema => JsonUtility.DeserializeData<JsonSchema>(fileResolver.ReadString(schema), schema.Source?.File));

            _schemaValidators = Array.ConvertAll(
                MetadataSchemas,
                schema => new JsonSchemaValidator(schema, microsoftGraphAccessor));

            _reservedMetadata = JsonUtility.GetPropertyNames(typeof(SystemMetadata))
                .Concat(JsonUtility.GetPropertyNames(typeof(ConceptualModel)))
                .Concat(MetadataSchemas.SelectMany(schema => schema.Reserved))
                .Except(JsonUtility.GetPropertyNames(typeof(UserMetadata)))
                .ToHashSet();
        }

        public void ValidateMetadata(ErrorBuilder errors, JObject metadata, FilePath filePath)
        {
            foreach (var (key, value) in metadata)
            {
                if (value is null)
                {
                    continue;
                }
                if (_reservedMetadata.Contains(key))
                {
                    errors.Add(Errors.Metadata.AttributeReserved(JsonUtility.GetKeySourceInfo(value), key));
                }
            }

            var contentType = _buildScope.GetContentType(filePath);
            var mime = _buildScope.GetMime(contentType, filePath);
            var siteUrl = _documentProvider.GetSiteUrl(filePath);
            var canonicalVersion = _publishUrlMap.GetCanonicalVersion(siteUrl);
            var isCanonicalVersion = _monikerProvider.GetFileLevelMonikers(errors, filePath).IsCanonicalVersion(canonicalVersion);

            foreach (var schemaValidator in _schemaValidators)
            {
                // Only validate conceptual files
                if (contentType == ContentType.Page && mime == "Conceptual" &&
                    (!metadata.ContainsKey("layout") ||
                     string.Equals(metadata.GetValue("layout")?.ToString(), "conceptual", StringComparison.OrdinalIgnoreCase)))
                {
                    errors.AddRange(schemaValidator.Validate(metadata, isCanonicalVersion));
                }
            }
        }

        public List<Error> PostValidate()
        {
            var errors = new List<Error>();
            foreach (var validator in _schemaValidators)
            {
                errors.AddRange(validator.PostValidate());
            }

            return errors;
        }
    }
}
