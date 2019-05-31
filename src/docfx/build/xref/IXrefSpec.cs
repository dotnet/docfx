// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal interface IXrefSpec
    {
        string Href { get; }

        Document DeclairingFile { get; }

        HashSet<Moniker> Monikers { get; }

        string GetXrefPropertyValue(string propertyName);
    }
}
