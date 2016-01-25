// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal sealed class MetadataSchema : IMetadataSchema
    {
        public IReadOnlyList<IWellknownMetadataValidator> Validators { get; }
        public IReadOnlyList<IUnknownMetadataValidator> UnknownValidators { get; }
        public IReadOnlyDictionary<string, IMetadataDefinition> Definitions { get; }

        public MetadataSchema(
            IReadOnlyDictionary<string, IMetadataDefinition> definitions = null,
            IReadOnlyList<IWellknownMetadataValidator> validators = null,
            IReadOnlyList<IUnknownMetadataValidator> unknownValidators = null)
        {
            Definitions = definitions;
            Validators = validators;
            UnknownValidators = unknownValidators;
        }

        public ValidationResults ValidateMetadata(string metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }
            return new ValidationResults(
                from rs in ValidateMetadataCore(JsonConvert.DeserializeObject<JObject>(metadata))
                from r in rs
                select r);
        }

        private IEnumerable<IEnumerable<ValidationResult>> ValidateMetadataCore(JObject obj)
        {
            foreach (var prop in obj)
            {
                IMetadataDefinition def;
                if (Definitions != null &&
                    Definitions.TryGetValue(prop.Key, out def))
                {
                    if (Validators != null)
                    {
                        yield return from v in Validators select v.Validate(prop.Key, def, prop.Value);
                    }
                }
                else
                {
                    if (UnknownValidators != null)
                    {
                        yield return from v in UnknownValidators select v.Validate(prop.Key, prop.Value);
                    }
                }
            }
        }
    }
}
