// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class TreeIterator
    {
        public static async Task PreorderAsync<T>(T current, T parent, Func<T, IEnumerable<T>> childrenGetter, Func<T, T, Task<bool>> action)
        {
            if (current == null || action == null)
            {
                return;
            }

            if (!await action(current, parent))
            {
                return;
            }

            if (childrenGetter == null)
            {
                return;
            }

            var children = childrenGetter(current);
            if (children != null)
            {
                foreach (var child in children)
                {
                    await PreorderAsync(child, current, childrenGetter, action);
                }
            }
        }

        public static void Preorder<T>(T current, T parent, Func<T, IEnumerable<T>> childrenGetter, Func<T, T, bool> action)
        {
            if (current == null || action == null)
            {
                return;
            }

            if (!action(current, parent))
            {
                return;
            }

            if (childrenGetter == null)
            {
                return;
            }

            var children = childrenGetter(current);
            if (children != null)
            {
                foreach (var child in children)
                {
                    Preorder(child, current, childrenGetter, action);
                }
            }
        }

        public static T PreorderFirstOrDefault<T>(T current, Func<T, IEnumerable<T>> childrenGetter, Func<T, bool> predicate)
        {
            if (predicate(current))
                return current;
            if (childrenGetter == null)
                return default(T);
            var children = childrenGetter(current);
            if (children == null)
                return default(T);
            foreach (var child in children)
            {
                var result = PreorderFirstOrDefault(child, childrenGetter, predicate);
                if (!object.Equals(result, default(T)))
                {
                    return result;
                }
            }

            return default(T);
        }
    }
}
