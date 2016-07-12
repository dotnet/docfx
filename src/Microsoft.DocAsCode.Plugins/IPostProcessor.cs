// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Immutable;

    public interface IPostProcessor
    {
        /// <summary>
        /// Update global metadata before building all the files declared in `docfx.json`
        /// </summary>
        ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata);

        /// <summary>
        /// Add/remove/update all the files included in manifest
        /// </summary>
        Manifest Process(Manifest manifest, string outputFolder);
    }
}
