// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal static class Errors
    {
        public static Exception ConfigNotFound(string docsetPath)
            => new DocfxException("config-not-found", $"Cannot find docfx.yml at '{docsetPath}'");

        public static Exception InvalidConfig(string configPath, Exception e)
            => new DocfxException("invalid-config", $"Error parsing docset config: {e.Message}", configPath, innerException: e);

        public static Exception CircularReference<T>(T filePath, IEnumerable<T> dependencyChain)
            => new DocfxException("circular-reference", $"Found circular reference: {string.Join(" --> ", dependencyChain)} --> {filePath}", filePath.ToString());
    }
}
