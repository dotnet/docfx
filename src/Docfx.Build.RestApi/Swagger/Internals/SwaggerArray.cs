// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.RestApi.Swagger.Internals;

internal class SwaggerArray : SwaggerObjectBase
{
    public override SwaggerObjectType ObjectType => SwaggerObjectType.Array;

    public List<SwaggerObjectBase> Array { get; set; } = [];

    public override SwaggerObjectBase Clone()
    {
        var clone = (SwaggerArray)MemberwiseClone();
        clone.Array = Array.Select(a => a.Clone()).ToList();
        clone.ReferencesResolved = false;
        return clone;
    }
}
