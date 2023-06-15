// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.DocAsCode.Common;

/// <summary>
///     A NETStandard-friendly replacement for CallContext.
/// </summary>
public static class LogicalCallContext
{
    static readonly ConcurrentDictionary<string, AsyncLocal<object>> _data = new();

    public static void SetData(string key, object data) => _data.GetOrAdd(key, _ => new AsyncLocal<object>()).Value = data;

    public static object GetData(string key) => _data.TryGetValue(key, out AsyncLocal<object> data) ? data.Value : null;
    public static void FreeData(string key) => _data.TryRemove(key, out _);
}
