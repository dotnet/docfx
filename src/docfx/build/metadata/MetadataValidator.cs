// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            var blackList = new HashSet<string>(JsonUtility.GetPropertyNames(typeof(PageModel)), StringComparer.OrdinalIgnoreCase);

            foreach (var name in JsonUtility.GetPropertyNames(typeof(FileMetadata)))
            {
                blackList.Remove(name);
            }

            return blackList;
        }
    }
}
