// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.RestApi.Swagger.Internals;

internal class SwaggerLoopReferenceObject : SwaggerObject
{
    public override SwaggerObjectType ObjectType => SwaggerObjectType.LoopReference;
}
