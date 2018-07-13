// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class BuildSchemaDocument
    {
        public static Task<(IEnumerable<Error> errors, PageModel result, DependencyMap dependencies)> Build()
        {
            return Task.FromResult((Enumerable.Empty<Error>(), (PageModel)null, DependencyMap.Empty));
        }
    }
}
