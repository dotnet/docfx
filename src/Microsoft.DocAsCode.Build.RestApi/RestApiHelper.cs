// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.RestApi.Swagger.Internals;
    using Microsoft.DocAsCode.Common;

    using Newtonsoft.Json.Linq;

    internal static class RestApiHelper
    {
        private const string JsonExtension = ".json";

        /// <summary>
        /// Reverse to reference unescape described in http://tools.ietf.org/html/rfc6901#section-4
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static string FormatDefinitionSinglePath(string reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }
            return reference.Replace("~", "~0").Replace("/", "~1");
        }

        /// <summary>
        /// When the reference starts with '#/', treat it as URI Fragment Identifier Representation and decode.
        /// When the reference starts with '/', treat it as JSON String Representation and keep it as.
        /// Refer to: https://tools.ietf.org/html/rfc6901#section-5
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static SwaggerFormattedReference FormatReferenceFullPath(string reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            // Decode for URI Fragment Identifier Representation
            if (reference.StartsWith("#/"))
            {
                var result = ParseReferencePath(reference.Substring(2));
                return new SwaggerFormattedReference
                {
                    Type = SwaggerFormattedReferenceType.InternalReference,
                    Path = "/" + result.Item1,
                    Name = result.Item2
                };
            }

            // Not decode for JSON String Representation
            if (reference.StartsWith("/"))
            {
                return new SwaggerFormattedReference
                {
                    Type = SwaggerFormattedReferenceType.InternalReference,
                    Path = reference,
                    Name = reference.Split('/').Last()
                };
            }

            // External reference
            if (PathUtility.IsRelativePath(reference))
            {
                // For example "file.json"
                if (reference.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return new SwaggerFormattedReference
                    {
                        Type = SwaggerFormattedReferenceType.ExternalReference,
                        ExternalFilePath = reference,
                        Name = Path.GetFileNameWithoutExtension(reference)
                    };
                }

                // For example "file.json#/definitions/reference"
                if (reference.Contains("#"))
                {
                    var values = reference.Split('#');
                    if (values.Length != 2)
                    {
                        throw new InvalidOperationException($"Reference path '{reference}' should contain only one '#' character.");
                    }
                    var filePath = values[0];
                    if (!filePath.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"External file path '{filePath}' should end with {JsonExtension}");
                    }
                    var parsedFilePath = ParseReferencePath(filePath).Item1;
                    var parsedReferencePath = ParseReferencePath(values[1].Substring(1));
                    return new SwaggerFormattedReference
                    {
                        Type = SwaggerFormattedReferenceType.ExternalEmbeddedReference,
                        ExternalFilePath = parsedFilePath,
                        Path = "/" + parsedReferencePath.Item1,
                        Name = parsedReferencePath.Item2
                    };
                }
            }

            throw new InvalidOperationException($"Reference path \"{reference}\" is not supported now.");
        }

        public static void CheckSpecificKey(JToken jToken, string key, Action action)
        {
            var jArray = jToken as JArray;
            if (jArray != null)
            {
                foreach (var item in jArray)
                {
                    CheckSpecificKey(item, key, action);
                }
            }

            var jObject = jToken as JObject;
            if (jObject != null)
            {
                foreach (var pair in jObject)
                {
                    if (pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        action();
                    }
                    CheckSpecificKey(pair.Value, key, action);
                }
            }
        }

        private static Tuple<string, string> ParseReferencePath(string path)
        {
            if (!RelativePath.IsRelativePath(path))
            {
                throw new InvalidOperationException($"{path} should be relative path.");
            }

            // Reuse relative path, to decode the values inside '/'.
            var decodedPath = ((RelativePath)path).UrlDecodeUnsafe();

            return Tuple.Create(decodedPath.ToString(), decodedPath.FileName);
        }
    }
}
