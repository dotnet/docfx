// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Common;

public interface ILoggerListener : IDisposable
{
    void WriteLine(ILogItem item);
    void Flush();
}
