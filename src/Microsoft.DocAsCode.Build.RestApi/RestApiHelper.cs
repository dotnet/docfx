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
                // Reuse relative path, to decode the values inside '/'.
                var path = reference.Substring(2);
                var decodedPath = ((RelativePath)path).UrlDecode();
                return new SwaggerFormattedReference
                {
                    Type = SwaggerFormattedReferenceType.InternalReference,
                    Path = "/" + decodedPath,
                    Name = decodedPath.FileName
                };
            }

            // External reference json
            if (reference.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return new SwaggerFormattedReference
                {
                    Type = SwaggerFormattedReferenceType.ExternalReference,
                    Path = reference,
                    Name = Path.GetFileNameWithoutExtension(reference)
                };
            }

            // Not decode for JSON String Representation
            if (reference.StartsWith("/"))
            {
                var fileName = reference.Split('/').Last();
                return new SwaggerFormattedReference
                {
                    Type = SwaggerFormattedReferenceType.InternalReference,
                    Path = reference,
                    Name = fileName
                };
            }

            throw new InvalidOperationException($"Reference path \"{reference}\" is not supported now");
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
    }
}
