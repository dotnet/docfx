// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal interface IXrefSpec
{
    string Uid { get; }

    string? SchemaType { get; }

    string Href { get; }

    FilePath? DeclaringFile { get; }

    MonikerList Monikers { get; }

    string? GetXrefPropertyValueAsString(string propertyName);

    string? GetName();

    ExternalXrefSpec ToExternalXrefSpec(string? overwriteHref = null, MonikerList? monikerList = null);
}
