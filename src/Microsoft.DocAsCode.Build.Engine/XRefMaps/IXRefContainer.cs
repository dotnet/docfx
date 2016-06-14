// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    public interface IXRefContainer
    {
        bool IsEmbeddedRedirections { get; }
        IEnumerable<XRefMapRedirection> GetRedirections();
        IXRefContainerReader GetReader();
    }
}
