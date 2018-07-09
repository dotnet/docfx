// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Errors
    {
        public static Error RedirectionConflict(string redirectFrom)
            => new Error(ErrorLevel.Error, "redirection-conflict", $"The '{redirectFrom}' appears twice or more in the redirection mappings");

        public static Error InvalidRedirection(string path, ContentType contentType)
            => new Error(ErrorLevel.Error, "invalid-redirection", $"The '{path}' shouldn't belong to redirections since it's a {contentType}");

        public static Error ConfigNotFound(string docsetPath)
            => new Error(ErrorLevel.Error, "config-not-found", $"Cannot find docfx.yml at '{docsetPath}'");

        public static Error InvalidConfig(string configPath, string message, Exception ex = null)
            => new Error(ErrorLevel.Error, "invalid-config", $"Error parsing docset config: {message ?? ex.Message}", configPath);

        public static Error InvalidLocale(string locale)
            => new Error(ErrorLevel.Error, "invalid-locale", $"Local {locale} is not supported ");

        public static Error CircularReference<T>(T filePath, IEnumerable<T> dependencyChain)
            => new Error(ErrorLevel.Error, "circular-reference", $"Found circular reference: {string.Join(" --> ", dependencyChain.Select(file => $"'{file}'"))} --> '{filePath}'", filePath.ToString());

        public static Error UserProfileCacheNotFound(string userProfilePath)
            => new Error(ErrorLevel.Error, "user-profile-cache-not-found", $"Cannot find user profile cache at '{userProfilePath}'");

        public static Error InvalidUserProfileCache(string userProfileCache, Exception ex)
            => new Error(ErrorLevel.Error, "invalid-user-profile-cache", ex.Message, userProfileCache);

        public static Error GitCommitsTimeNotFound(string gitCommitsTimePath)
            => new Error(ErrorLevel.Error, "git-commits-time-not-found", $"Cannot find git commits time at '{gitCommitsTimePath}'");

        public static Error InvalidGitCommitsTime(string gitCommitsTimePath, Exception ex)
            => new Error(ErrorLevel.Error, "invalid-git-commits-time", ex.Message, gitCommitsTimePath);

        public static Error AuthorNotFound(string author)
            => new Error(ErrorLevel.Warning, "author-not-found", $"Author '{author}' cannot be recognized");

        public static Error InvalidTopicHref(string topicHref)
            => new Error(ErrorLevel.Error, "invalid-topic-href", $"The topic href '{topicHref}' can only reference to a local file or absolute path");

        public static Error InvalidTocHref(string tocHref)
            => new Error(ErrorLevel.Error, "invalid-toc-href", $"The toc href '{tocHref}' can only reference to a local TOC file, folder or absolute path");

        public static Error YamlHeaderNotObject(object filePath, bool isArray)
            => new Error(ErrorLevel.Warning, "yaml-header-not-object", $"Expect yaml header to be an object, but got {(isArray ? "an array" : "a scalar")}", filePath.ToString());

        public static Error YamlSyntaxError(Exception ex)
            => new Error(ErrorLevel.Error, "yaml-syntax-error", ex.Message);

        public static Error InvalidYamlHeader(Document file, Exception ex)
            => new Error(ErrorLevel.Warning, "invalid-yaml-header", ex.Message, file.ToString());

        public static Error JsonSyntaxError(Exception ex)
            => new Error(ErrorLevel.Error, "json-syntax-error", ex.Message);

        public static Error LinkIsEmpty(Document file)
            => new Error(ErrorLevel.Info, "link-is-empty", "Link is empty", file.ToString());

        public static Error LinkOutOfScope(Document relativeTo, Document file, string href)
            => new Error(ErrorLevel.Warning, "link-out-of-scope", $"File '{file}' referenced by link '{href}' will not be build because it is not included in docfx.yml", relativeTo.ToString());

        public static Error LinkIsDependency(Document relativeTo, Document file, string href)
            => new Error(ErrorLevel.Warning, "link-is-dependency", $"File '{file}' referenced by link '{href}' will not be build because it is from a dependency docset", relativeTo.ToString());

        public static Error AbsoluteFilePath(Document relativeTo, string path)
            => new Error(ErrorLevel.Warning, "absolute-file-path", $"File path cannot be absolute: '{path}'", relativeTo.ToString());

        public static Error FileNotFound(Document relativeTo, string path)
            => new Error(ErrorLevel.Warning, "file-not-found", $"Cannot find file '{path}' relative to '{relativeTo}'", relativeTo.ToString());

        public static Error UidNotFound(Document file, string uid, string rawXref)
            => new Error(ErrorLevel.Warning, "uid-not-found", $"Cannot find uid '{uid}' using xref '{rawXref}'", file.ToString());

        public static Error AtUidNotFound(Document file, string uid, string rawXref)
            => new Error(ErrorLevel.Info, "at-uid-not-found", $"Cannot find uid '{uid}' using xref '{rawXref}'", file.ToString());

        public static Error PublishUrlConflict(string url, IEnumerable<Document> files)
            => new Error(ErrorLevel.Warning, "publish-url-conflict", $"Two or more documents publish to the same url '{url}': {string.Join(", ", files.OrderBy(file => file.FilePath).Select(file => file.ContentType == ContentType.Redirection ? $"'{file} <redirection>'" : $"'{file}'").Take(5))}");

        public static Error IncludeRedirection(Document relativeTo, string path)
            => new Error(ErrorLevel.Warning, "include-is-redirection", $"Referenced inclusion {path} relative to '{relativeTo}' shouldn't belong to redirections", relativeTo.ToString());

        public static Error OutputPathConflict(string path, IEnumerable<Document> files)
            => new Error(ErrorLevel.Warning, "output-path-conflict", $"Two or more documents output to the same path '{path}': {string.Join(", ", files.OrderBy(file => file.FilePath).Select(file => $"'{file}'").Take(5))}");

        public static Error RedirectionDocumentIdConflict(IEnumerable<Document> redirectFromDocs, string redirectTo)
            => new Error(ErrorLevel.Warning, "redirected-id-conflict", $"Multiple documents redirected to '{redirectTo}' with document id: {string.Join(", ", redirectFromDocs.OrderBy(f => f.FilePath).Select(f => $"'{f}'"))}");

        public static Error GitShadowClone(string repoPath)
            => new Error(ErrorLevel.Error, "git-shadow-clone", $"Does not support git shallow clone: '{repoPath}'");

        public static Error GitNotFound()
            => new Error(ErrorLevel.Error, "git-not-found", $"Cannot find git, install git https://git-scm.com/");

        public static Error NullValue(Range range)
            => new Error(ErrorLevel.Warning, "null-value", $"{range} contains null value");
    }
}
