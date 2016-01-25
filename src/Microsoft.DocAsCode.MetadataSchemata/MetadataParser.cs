// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    using Microsoft.DocAsCode.MetadataSchemata.SchemaValidators;

    public static class MetadataParser
    {
        private static readonly IMetadataSchema Schema =
            new MetadataSchema(
                unknownValidators: new List<IUnknownMetadataValidator>
                {
                    new UnknownNamingValidator(),
                    new DefinitionObjectValidator(),
                });

        public static IMetadataSchema GetMetadataSchema()
        {
            return Schema;
        }

        public static IMetadataSchema Load(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Content cannot be empty or white space.", nameof(content));
            }
            return LoadCore(content);
        }

        private static IMetadataSchema LoadCore(string content) =>
            new MetadataSchema(
                JsonConvert.DeserializeObject<Dictionary<string, MetadataDefinition>>(content)
                .ToDictionary(
                    x => x.Key,
                    x => (IMetadataDefinition)x.Value),
                new List<IWellknownMetadataValidator>
                {
                    new WellknownTypeValidator()
                        .Then(new WellknownChoiceSetValidator()),
                },
                new List<IUnknownMetadataValidator>
                {
                    new UnknownNamingValidator(),
                    new UnknownTypeValidator(),
                });
    }
}
