// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

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
            return default;
        var children = childrenGetter(current);
        if (children == null)
            return default;
        foreach (var child in children)
        {
            var result = PreorderFirstOrDefault(child, childrenGetter, predicate);
            if (!object.Equals(result, default(T)))
            {
                return result;
            }
        }

        return default;
    }
}
