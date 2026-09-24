// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.RestApi.Swagger;
using Docfx.DataContracts.RestApi;

namespace Docfx.Build.RestApi;

// Public compatibility entry point for callers consuming the original Swagger metadata contract.
public static partial class SwaggerModelConverter
{
    public static RestApiRootItemViewModel FromSwaggerModel(SwaggerModel swagger) =>
        OpenApi2ModelConverter.ConvertLegacy(swagger);
}
