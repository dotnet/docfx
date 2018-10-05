// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal static class CollectionUtility
    {
        public static void AddIfNotNull<T>(this IList<T> list, T value) where T : class
        {
            if (!(value is null))
            {
                list.Add(value);
            }
        }
    }
}
