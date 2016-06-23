// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;

    using Microsoft.DocAsCode.Utility;

    public static class RestApiHelper
    {
        /// <summary>
        /// Use JSON String Representation instead of URI Fragment Identifier Representation, refer to: https://tools.ietf.org/html/rfc6901#section-5
        /// Reverse to reference unescape described in http://tools.ietf.org/html/rfc6901#section-4
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static string FormatDefinitionSinglePath(string reference)
        {
            return Uri.UnescapeDataString(reference.Replace("~", "~0").Replace("/", "~1"));
        }

        public static string FormatReferenceFullPath(string reference)
        {
            if (reference.StartsWith("/"))
            {
                reference = reference.Substring(1);
            }
            else if (reference.StartsWith("#/"))
            {
                reference = reference.Substring(2);
            }
            else
            {
                throw new ArgumentException($"Full reference path \"{reference}\" must start with '/' or '#/'");
            }

            // Reuse relative path, to decode the values inside '/'.
            return "/" + ((RelativePath)reference).UrlDecode();
        }
    }
}
