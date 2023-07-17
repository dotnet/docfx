// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;

namespace Docfx.Build.RestApi.Swagger.Internals;

internal class SwaggerValue : SwaggerObjectBase
{
    public override SwaggerObjectType ObjectType => SwaggerObjectType.ValueType;

    public JToken Token { get; set; }

    public override SwaggerObjectBase Clone()
    {
        var clone = (SwaggerValue)MemberwiseClone();
        clone.ReferencesResolved = false;
        return clone;
    }
}
