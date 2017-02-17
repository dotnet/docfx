// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    using System;

    using Microsoft.DocAsCode.Common;

    internal class JsonLocationInfo
    {
        public string FilePath { get; }

        public string JsonLocation { get; }

        public JsonLocationInfo(string swaggerPath, string jsonLocation)
        {
            FilePath = PathUtility.NormalizePath(swaggerPath);
            JsonLocation = jsonLocation;
        }

        public override bool Equals(object obj)
        {
            var other = obj as JsonLocationInfo;
            if (other == null)
            {
                return false;
            }

            return FilePathComparer.OSPlatformSensitiveStringComparer.Equals(FilePath, other.FilePath)
                && string.Equals(JsonLocation, other.JsonLocation);
        }

        public override int GetHashCode()
        {
            var result = 17;
            result = result * 23 + FilePathComparer.OSPlatformSensitiveStringComparer.GetHashCode(FilePath);
            result = result * 23 + JsonLocation.GetHashCode();
            return result;
        }
    }
}
