// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    public interface ICompositionContainer
    {
        T GetExport<T>();
        T GetExport<T>(string name);
        IEnumerable<T> GetExports<T>();
        IEnumerable<T> GetExports<T>(string name);
    }
}
