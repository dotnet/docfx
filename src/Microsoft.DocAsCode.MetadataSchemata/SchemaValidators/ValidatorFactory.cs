// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata.SchemaValidators
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    public static class ValidatorFactory
    {
        public static IWellknownMetadataValidator Then(this IWellknownMetadataValidator validator, IWellknownMetadataValidator next)
        {
            if (validator == null)
            {
                throw new ArgumentNullException(nameof(validator));
            }
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            var chain = validator as ChainWellknownValidator;
            if (chain == null)
            {
                return new ChainWellknownValidator
                {
                    Validators =
                    {
                        validator,
                        next,
                    }
                };
            }
            chain.Validators.Add(next);
            return chain;
        }

        private sealed class ChainWellknownValidator : IWellknownMetadataValidator
        {
            public List<IWellknownMetadataValidator> Validators { get; } = new List<IWellknownMetadataValidator>();

            public ValidationResult Validate(string path, IMetadataDefinition definition, JToken value)
            {
                foreach (var v in Validators)
                {
                    var result = v.Validate(path, definition, value);
                    if (result != ValidationResult.Success)
                    {
                        return result;
                    }
                }
                return ValidationResult.Success;
            }
        }
    }
}
