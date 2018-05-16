// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal static class Errors
    {
        public static DocfxException ConfigNotFound(string docsetPath)
            => new DocfxException("config-not-found", $"Cannot find docfx.yml at '{docsetPath}'");

        public static DocfxException InvalidConfig(string configPath, Exception e)
            => new DocfxException("invalid-config", $"Error parsing docset config: {e.Message}", configPath, innerException: e);

        public static DocfxException CircularReference<T>(T filePath, IEnumerable<T> dependencyChain)
            => new DocfxException("circular-reference", $"Found circular reference: {string.Join(" --> ", dependencyChain)} --> {filePath}", filePath.ToString());

        public static DocfxException YamlHeaderNotObject(object filePath, bool isArray)
            => new DocfxException("yaml-header-not-object", $"Expect yaml header to be an object, but got {(isArray ? "an array" : "a scalar")}", filePath.ToString());

        public static DocfxException InvalidYamlHeader(Document file, Exception ex)
            => new DocfxException("invalid-yaml-header", ex.Message, file.ToString());
    }
}
