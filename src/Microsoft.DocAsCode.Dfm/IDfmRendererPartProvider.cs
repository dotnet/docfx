// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    public interface IDfmRendererPartProvider
    {
        IEnumerable<IDfmRendererPart> CreateParts(IReadOnlyDictionary<string, object> paramters);
    }
}
