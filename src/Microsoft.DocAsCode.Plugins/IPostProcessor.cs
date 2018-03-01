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
        /// <param name="metadata"></param>
        /// <returns></returns>
        ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata);

        /// <summary>
        /// Add/remove/update all the files included in manifest
        /// </summary>
        /// <param name="manifest"></param>
        /// <param name="outputFolder">The output folder where our static website will be placed</param>
        /// <returns></returns>
        Manifest Process(Manifest manifest, string outputFolder);
    }
}
