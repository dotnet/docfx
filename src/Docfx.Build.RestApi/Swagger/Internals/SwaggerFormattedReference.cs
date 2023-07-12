// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.RestApi.Swagger.Internals;

internal class SwaggerFormattedReference
{
    public SwaggerFormattedReferenceType Type { get; set; }

    public string ExternalFilePath { get; set; }

    public string Path { get; set; }

    public string Name { get; set; }
}

internal enum SwaggerFormattedReferenceType
{
    InternalReference,
    ExternalEmbeddedReference,
    ExternalReference
}
