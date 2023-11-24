// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.RestApi.Swagger.Internals;

internal abstract class SwaggerObjectBase
{
    public abstract SwaggerObjectType ObjectType { get; }

    public bool ReferencesResolved { get; set; }

    public string Location { get; set; }

    public abstract SwaggerObjectBase Clone();
}
