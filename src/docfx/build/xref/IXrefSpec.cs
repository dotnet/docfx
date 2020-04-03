// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal interface IXrefSpec
    {
        string Uid { get; }

        string Href { get; }

        string Name { get; }

        Document? DeclaringFile { get; }

        HashSet<string> Monikers { get; }

        string? GetXrefPropertyValueAsString(string propertyName);

        ExternalXrefSpec ToExternalXrefSpec(string? overwriteHref = null);
    }
}
