// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;

namespace Docfx.Build.RestApi.Swagger.Internals;

internal class SwaggerReferenceObject : SwaggerObjectBase
{
    public override SwaggerObjectType ObjectType => SwaggerObjectType.ReferenceObject;

    public string DeferredReference { get; set; }

    public string ReferenceName { get; set; }

    public string ExternalFilePath { get; set; }

    public JObject Token { get; set; }

    public SwaggerObject Reference { get; set; }

    public override SwaggerObjectBase Clone()
    {
        var clone = (SwaggerReferenceObject)MemberwiseClone();
        clone.Reference = (SwaggerObject)Reference?.Clone();
        clone.ReferencesResolved = false;
        return clone;
    }
}
