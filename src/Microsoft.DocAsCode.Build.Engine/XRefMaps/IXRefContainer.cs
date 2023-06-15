// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Build.Engine;

public interface IXRefContainer
{
    bool IsEmbeddedRedirections { get; }
    IEnumerable<XRefMapRedirection> GetRedirections();
    IXRefContainerReader GetReader();
}
