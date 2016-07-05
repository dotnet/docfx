// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;
    using System.Linq;

    using Microsoft.DocAsCode.Utility;

    public static class RestApiHelper
    {
        /// <summary>
        /// Reverse to reference unescape described in http://tools.ietf.org/html/rfc6901#section-4
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static string FormatDefinitionSinglePath(string reference)
        {
            return reference.Replace("~", "~0").Replace("/", "~1");
        }

        /// <summary>
        /// When the reference starts with '#/', treat it as URI Fragment Identifier Representation and decode.
        /// When the reference starts with '/', treat it as JSON String Representation and keep it as.
        /// Refer to: https://tools.ietf.org/html/rfc6901#section-5
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Tuple<string, string> FormatReferenceFullPath(string reference)
        {
            // Decode for URI Fragment Identifier Representation
            if (reference.StartsWith("#/"))
            {
                // Reuse relative path, to decode the values inside '/'.
                var path = reference.Substring(2);
                var decodedPath = ((RelativePath)path).UrlDecode();
                return Tuple.Create("/" + decodedPath, decodedPath.FileName);
            }

            // Not decode for JSON String Representation
            if (reference.StartsWith("/"))
            {
                var fileName = reference.Split('/').Last();
                return Tuple.Create(reference, fileName);
            }

            throw new InvalidOperationException($"Full reference path \"{reference}\" must start with '/' or '#/'");
        }
    }
}
