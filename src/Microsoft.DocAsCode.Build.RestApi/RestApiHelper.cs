// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

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
        public static SwaggerReference FormatReferenceFullPath(string reference)
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
                return new SwaggerReference
                {
                    Type = SwaggerReferenceType.InternalReference,
                    Path = "/" + decodedPath,
                    Name = decodedPath.FileName
                };
            }

            // External reference json
            if (reference.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return new SwaggerReference
                {
                    Type = SwaggerReferenceType.ExternalReference,
                    Path = reference,
                    Name = Path.GetFileNameWithoutExtension(reference)
                };
            }

            // Not decode for JSON String Representation
            if (reference.StartsWith("/"))
            {
                var fileName = reference.Split('/').Last();
                return new SwaggerReference
                {
                    Type = SwaggerReferenceType.InternalReference,
                    Path = reference,
                    Name = fileName
                };
            }

            throw new InvalidOperationException($"Reference path \"{reference}\" is not supported now");
        }

        public class SwaggerReference
        {
            public SwaggerReferenceType Type { get; set; }

            public string Path { get; set; }

            public string Name { get; set; }
        }

        public enum SwaggerReferenceType
        {
            InternalReference,
            ExternalReference
        }
    }
}
