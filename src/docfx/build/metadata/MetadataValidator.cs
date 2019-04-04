// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class MetadataValidator
    {
        private static readonly HashSet<string> s_reservedNames = GetReservedMetadata();
        private static readonly ConcurrentDictionary<string, Lazy<Type>> s_fileMetadataTypes = new ConcurrentDictionary<string, Lazy<Type>>(StringComparer.OrdinalIgnoreCase);

        public static List<Error> ValidateFileMetadata(JObject metadata)
        {
            var errors = new List<Error>();
            if (metadata is null)
                return errors;

            foreach (var (key, token) in metadata)
            {
                var lineInfo = token as IJsonLineInfo;
                if (s_reservedNames.Contains(key))
                {
                    errors.Add(Errors.ReservedMetadata(new SourceInfo(null, lineInfo?.LineNumber ?? 0, lineInfo?.LinePosition ?? 0), key, token.Path));
                }
                else
                {
                    var type = s_fileMetadataTypes.GetOrAdd(
                       key,
                       new Lazy<Type>(() => typeof(FileMetadata).GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.PropertyType));
                    if (type.Value is null)
                        continue;
                    var values = token as IEnumerable<KeyValuePair<string, JToken>>;
                    foreach (var (glob, value) in values)
                    {
                        var nestedLineInfo = value as IJsonLineInfo;
                        if (!type.Value.IsInstanceOfType(value))
                        {
                            errors.Add(Errors.ViolateSchema(
                                new SourceInfo(null, nestedLineInfo?.LineNumber ?? 0, nestedLineInfo?.LinePosition ?? 0),
                                $"Expected type {type.Value.Name}, please input string or type compatible with {type.Value.Name}."));
                        }
                    }
                }
            }

            return errors;
        }

        public static List<Error> ValidateGlobalMetadata(JObject metadata)
        {
            var errors = new List<Error>();
            if (metadata is null)
                return errors;

            foreach (var (key, token) in metadata)
            {
                if (s_reservedNames.Contains(key))
                {
                    errors.Add(Errors.ReservedMetadata(JsonUtility.ToSourceInfo(token), key, token.Path));
                }
            }

            if (!errors.Any())
            {
                var (schemaErrors, _) = JsonUtility.ToObject<FileMetadata>(metadata);
                errors.AddRange(schemaErrors);
            }
            return errors;
        }

        private static HashSet<string> GetReservedMetadata()
        {
            var blackList = new HashSet<string>(JsonUtility.GetPropertyNames(typeof(PageModel)));

            foreach (var name in JsonUtility.GetPropertyNames(typeof(FileMetadata)))
            {
                blackList.Remove(name);
            }

            return blackList;
        }
    }
}
