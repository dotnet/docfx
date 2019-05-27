// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal interface IXrefSpec
    {
        string Href { get; }

        Document ReferencedFile { get; }

        HashSet<string> Monikers { get; }

        string GetXrefPropertyValue(string propertyName);
    }
}
