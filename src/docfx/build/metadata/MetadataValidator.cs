// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    internal static class MetadataValidator
    {
        private static readonly HashSet<string> _reservedNames = GetReservedMetadata();

        public static List<Error> Validate<T>(IEnumerable<KeyValuePair<string, T>> metadata, string from)
        {
            var errors = new List<Error>();

            foreach (var (key, token) in metadata)
            {
                var lineInfo = token as IJsonLineInfo;
                if (_reservedNames.Contains(key))
                {
                    errors.Add(Errors.ReservedMetadata(new Range(lineInfo?.LineNumber ?? 0, lineInfo?.LinePosition ?? 0), key, from));
                }
            }

            return errors;
        }

        public static List<Error> Validate(JObject metadata, string from)
            => Validate((IEnumerable<KeyValuePair<string, JToken>>)metadata, from);

        private static HashSet<string> GetReservedMetadata()
        {
            var blackList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var outputProperties = ((JsonObjectContract)JsonUtility.DefaultSerializer.ContractResolver.ResolveContract(typeof(PageModel))).Properties;
            foreach (var property in outputProperties)
            {
                blackList.Add(property.PropertyName);
            }

            var inputProperties = ((JsonObjectContract)JsonUtility.DefaultSerializer.ContractResolver.ResolveContract(typeof(FileMetadata))).Properties;
            foreach (var property in inputProperties)
            {
                blackList.Remove(property.PropertyName);
            }

            return blackList;
        }
    }
}
