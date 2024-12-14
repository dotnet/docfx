// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.RestApi.Swagger.Internals;

internal class SwaggerObject : SwaggerObjectBase
{
    public override SwaggerObjectType ObjectType => SwaggerObjectType.Object;

    public Dictionary<string, SwaggerObjectBase> Dictionary { get; set; } = [];

    public override SwaggerObjectBase Clone()
    {
        var clone = (SwaggerObject)MemberwiseClone();
        clone.Dictionary = Dictionary.ToDictionary(k => k.Key, k => k.Value.Clone());
        clone.ReferencesResolved = false;
        return clone;
    }
}
