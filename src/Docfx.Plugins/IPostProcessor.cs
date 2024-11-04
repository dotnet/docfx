// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Docfx.Plugins;

public interface IPostProcessor
{
    /// <summary>
    /// Update global metadata before building all the files declared in `docfx.json`
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata);

    /// <summary>
    /// Add/remove/update all the files included in manifest
    /// </summary>
    /// <param name="manifest"></param>
    /// <param name="outputFolder">The output folder where our static website will be placed</param>
    /// <param name="cancellationToken">The token to cancel operation.</param>
    /// <returns></returns>
    Manifest Process(Manifest manifest, string outputFolder, CancellationToken cancellationToken);
}
