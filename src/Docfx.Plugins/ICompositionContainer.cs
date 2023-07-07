// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Plugins;

public interface ICompositionContainer
{
    T GetExport<T>();
    T GetExport<T>(string name);
    IEnumerable<T> GetExports<T>();
    IEnumerable<T> GetExports<T>(string name);
}
