// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    public interface IBuildParameters
    {
        IReadOnlyDictionary<string, JArray> TagParameters { get; }
    }
}
