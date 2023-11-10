// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.Build.RestApi.Swagger.Internals;

internal class JsonLocationInfo
{
    public string FilePath { get; }

    public string JsonLocation { get; }

    public JsonLocationInfo(string swaggerPath, string jsonLocation)
    {
        FilePath = Path.GetFullPath(swaggerPath).Replace('\\', '/');
        JsonLocation = jsonLocation;
    }

    public override bool Equals(object obj)
    {
        if (obj is not JsonLocationInfo other)
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
