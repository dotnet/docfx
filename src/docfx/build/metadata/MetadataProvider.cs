// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class MetadataProvider
    {
        private readonly Config _config;
        private readonly List<(Func<string, bool> glob, string key, JToken value)> _rules = new List<(Func<string, bool> glob, string key, JToken value)>();

        public MetadataProvider(Config config)
        {
            _config = config;

            foreach (var (key, item) in config.FileMetadata.ToObject<Dictionary<string, Dictionary<string, JToken>>>())
            {
                foreach (var (glob, value) in item)
                {
                    _rules.Add((GlobUtility.CreateGlobMatcher(glob), key, value));
                }
            }
        }

        public (List<Error> errors, JObject metadata) GetMetadata(Document file, JObject yamlHeader = null)
        {
            Debug.Assert(file != null);

            var erros = new List<Error>();
            var fileMetadata = new JObject();
            foreach (var (glob, key, value) in _rules)
            {
                if (glob(file.FilePath))
                {
                    fileMetadata[key] = value;
                }
            }

            var result = new JObject();
            result.Merge(_config.GlobalMetadata, JsonUtility.MergeSettings);
            result.Merge(fileMetadata, JsonUtility.MergeSettings);

            if (yamlHeader != null)
            {
                erros.AddRange(MetadataValidator.Validate(yamlHeader, "yaml header"));
                result.Merge(yamlHeader, JsonUtility.MergeSettings);
            }

            return (erros, result);
        }

        public (List<Error> errors, FileMetadata fileMetadata) GetFileMetadata(Document file, JObject yamlHeader = null)
        {
            var errors = new List<Error>();
            var (metaErrors, metadata) = GetMetadata(file, yamlHeader);
            errors.AddRange(metaErrors);

            var (fileMetaErrors, fileMetadata) = JsonUtility.ToObjectWithSchemaValidation<FileMetadata>(metadata);
            errors.AddRange(fileMetaErrors);

            return (errors, fileMetadata);
        }
    }
}
