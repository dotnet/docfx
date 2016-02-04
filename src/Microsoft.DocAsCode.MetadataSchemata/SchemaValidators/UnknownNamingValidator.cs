// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata.SchemaValidators
{
    using System.Text.RegularExpressions;

    using Newtonsoft.Json.Linq;

    public class UnknownNamingValidator : IUnknownMetadataValidator
    {
        private static readonly Regex Regex =
#if NetCore
            new Regex("^[a-z][a-z0-9_]*$");
#else
            new Regex("^[a-z][a-z0-9_]*$", RegexOptions.Compiled);
#endif

        public ValidationResult Validate(string name, JToken value)
        {
            if (Regex.IsMatch(name))
            {
                return ValidationResult.Success;
            }
            else
            {
                return ValidationResult.Fail(ValidationErrorCodes.UnknownMetadata.BadNaming, $"Bad naming for unknown metadata {name}.", name);
            }
        }
    }
}
