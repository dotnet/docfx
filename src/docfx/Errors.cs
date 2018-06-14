// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Errors
    {
        public static DocfxException InvalidRedirection(Document document)
            => new DocfxException(ReportLevel.Error, "invalid-redirection", $"The {document.FilePath} shouldn't belong to redirections since it's a {document.ContentType}");

        public static DocfxException ConfigNotFound(string docsetPath)
            => new DocfxException(ReportLevel.Error, "config-not-found", $"Cannot find docfx.yml at '{docsetPath}'");

        public static DocfxException InvalidConfig(string configPath, Exception e)
            => new DocfxException(ReportLevel.Error, "invalid-config", $"Error parsing docset config: {e.Message}", configPath, innerException: e);

        public static DocfxException CircularReference<T>(T filePath, IEnumerable<T> dependencyChain)
            => new DocfxException(ReportLevel.Error, "circular-reference", $"Found circular reference: {string.Join(" --> ", dependencyChain.Select(file => $"'{file}'"))} --> '{filePath}'", filePath.ToString());

        public static DocfxException YamlHeaderNotObject(object filePath, bool isArray)
            => new DocfxException(ReportLevel.Warning, "yaml-header-not-object", $"Expect yaml header to be an object, but got {(isArray ? "an array" : "a scalar")}", filePath.ToString());

        public static DocfxException InvalidYamlHeader(Document file, Exception ex)
            => new DocfxException(ReportLevel.Warning, "invalid-yaml-header", ex.Message, file.ToString());

        public static DocfxException LinkIsEmpty(Document file)
            => new DocfxException(ReportLevel.Warning, "link-is-empty", "Link is empty", file.ToString());

        public static DocfxException AbsoluteFilePath(Document relativeTo, string path)
            => new DocfxException(ReportLevel.Warning, "absolute-file-path", $"File path cannot be absolute: '{path}'", relativeTo.ToString());

        public static DocfxException FileNotFound(Document relativeTo, string path)
            => new DocfxException(ReportLevel.Warning, "file-not-found", $"Cannot find file '{path}' relative to '{relativeTo}'", relativeTo.ToString());

        public static DocfxException PublishUrlConflict(string url, IEnumerable<Document> files)
            => new DocfxException(ReportLevel.Warning, "publish-url-conflict", $"Two or more documents uses the same url '{url}': {string.Join(", ", files.OrderBy(file => file.FilePath).Select(file => file.IsRedirection ? $"'{file}(redirection)'" : $"'{file}'").Take(5))}");

        public static DocfxException IncludeRedirection(Document relativeTo, string path)
            => new DocfxException(ReportLevel.Warning, "include-is-redirection", $"Referenced inclusion {path} relative to '{relativeTo}' shouldn't belong to redirections", relativeTo.ToString());
    }
}
