// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine;

public sealed class XRefMapReader : XRefRedirectionReader
{
    private readonly Dictionary<string, IXRefContainer> _maps;

    public XRefMapReader(string majorKey, Dictionary<string, IXRefContainer> maps)
        : base(majorKey, new HashSet<string>(maps.Keys))
    {
        _maps = maps;
    }

    protected override IXRefContainer GetMap(string name)
    {
        _maps.TryGetValue(name, out IXRefContainer result);
        return result;
    }
}
