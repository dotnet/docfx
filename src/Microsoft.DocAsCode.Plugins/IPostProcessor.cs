// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Immutable;

    public interface IPostProcessor
    {
        string Name { get; }

        ImmutableDictionary<string, object> Update(ImmutableDictionary<string, object> metadata);

        Manifest Process(Manifest manifest, string baseDir);
    }
}
