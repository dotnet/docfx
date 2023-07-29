// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Docfx.Common;

public class LogCodesLogListener : ILoggerListener
{
    public ConcurrentDictionary<string, ImmutableHashSet<string>> Codes { get; }
        = new ConcurrentDictionary<string, ImmutableHashSet<string>>(FilePathComparer.OSPlatformSensitiveRelativePathComparer);

    public void Dispose()
    {
    }

    public void Flush()
    {
    }

    public void WriteLine(ILogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (string.IsNullOrEmpty(item.File) || string.IsNullOrEmpty(item.Code))
        {
            return;
        }
        Codes.AddOrUpdate(
            item.File,
            ImmutableHashSet.Create(item.Code),
            (_, v) => v.Add(item.Code));
    }
}
