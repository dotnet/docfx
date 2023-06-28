// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals;

internal class SwaggerArray : SwaggerObjectBase
{
    public override SwaggerObjectType ObjectType => SwaggerObjectType.Array;

    public List<SwaggerObjectBase> Array { get; set; } = new List<SwaggerObjectBase>();

    public override SwaggerObjectBase Clone()
    {
        var clone = (SwaggerArray)MemberwiseClone();
        clone.Array = Array.Select(a => a.Clone()).ToList();
        clone.ReferencesResolved = false;
        return clone;
    }
}
