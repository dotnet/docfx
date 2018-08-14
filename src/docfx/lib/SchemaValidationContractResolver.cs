// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    internal sealed class SchemaValidationContractResolver : JsonContractResolver
    {
        private readonly List<Error> _errors;

        public SchemaValidationContractResolver(List<Error> errors)
        {
            _errors = errors;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);
            var converter = GetConverter(member, _errors);

            if (converter != null)
            {
                if (prop.PropertyType.IsArray)
                {
                    prop.ItemConverter = converter;
                }
                else
                {
                    prop.Converter = converter;
                }
            }
            return prop;
        }

        private SchemaValidationConverter GetConverter(MemberInfo member, List<Error> errors)
        {
            var validators = member.GetCustomAttributes<ValidationAttribute>(false);
            return validators.Any() ? null : new SchemaValidationConverter(validators, errors);
        }
    }
}
