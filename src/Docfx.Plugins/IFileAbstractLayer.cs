// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Plugins;

public interface IFileAbstractLayer
{
    IEnumerable<string> GetAllInputFiles();

    bool Exists(string file);

    Stream OpenRead(string file);

    Stream Create(string file);

    void Copy(string sourceFileName, string destFileName);

    string GetPhysicalPath(string file);

    string GetExpectedPhysicalPath(string file);
}
