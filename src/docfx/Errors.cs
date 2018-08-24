// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;

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

        public static Error CircularReference<T>(T filePath, IEnumerable<T> dependencyChain)
            => new Error(ErrorLevel.Error, "circular-reference", $"Found circular reference: {string.Join(" --> ", dependencyChain.Select(file => $"'{file}'"))} --> '{filePath}'", filePath.ToString());

        public static Error UrlRestorePathNotFound(string url)
            => new Error(ErrorLevel.Error, "url-restore-path-not-found", $"The restore path of url `{url}` can't be found, make sure the `restore` command was executed");

        public static Error InvalidUserProfileCache(string userProfileCache, Exception ex)
            => new Error(ErrorLevel.Error, "invalid-user-profile-cache", ex.Message, userProfileCache);

        public static Error InvalidGitCommitsTime(string gitCommitsTimePath, Exception ex)
            => new Error(ErrorLevel.Error, "invalid-git-commits-time", ex.Message, gitCommitsTimePath);

        public static Error DependenyRepoNotFound(string dependenyRepoHref)
            => new Error(ErrorLevel.Error, "dependency-repo-not-found", $"The dependency repository with href '{dependenyRepoHref}' is not found, make sure the `restore` command was executed");

        public static Error AuthorNotFound(string author)
            => new Error(ErrorLevel.Warning, "author-not-found", $"Cannot find user '{author}' on GitHub");

        public static Error ResolveAuthorFailed(string author, string message)
            => new Error(ErrorLevel.Warning, "resolve-author-failed", $"Resolve user '{author}' from GitHub failed: {message}");

        public static Error ResolveCommitFailed(string sha, string repo, string message)
            => new Error(ErrorLevel.Warning, "resolve-commit-failed", $"Resolve commit '{sha}' of repository '{repo}' from GitHub failed: {message}");

        public static Error InvalidTopicHref(Document relativeTo, string topicHref)
            => new Error(ErrorLevel.Error, "invalid-topic-href", $"The topic href '{topicHref}' can only reference to a local file or absolute path", relativeTo.ToString());

        public static Error InvalidTocHref(Document relativeTo, string tocHref)
            => new Error(ErrorLevel.Error, "invalid-toc-href", $"The toc href '{tocHref}' can only reference to a local TOC file, folder or absolute path", relativeTo.ToString());

        public static Error MissingTocHead(Range range, string filePath)
            => new Error(ErrorLevel.Error, "missing-toc-head", $"The toc head name is missing", filePath, range);

        public static Error InvalidTocSyntax(Range range, string filePath, string syntax)
            => new Error(ErrorLevel.Error, "invalid-toc-syntax", $"The toc syntax '{syntax}' is invalided", filePath, range);

        public static Error InvalidTocLevel(string filePath, int from, int to)
            => new Error(ErrorLevel.Error, "invalid-toc-level", $"The toc level can't be skipped from {from} to {to}", filePath);

        public static Error DownloadFailed(string url, string message)
            => new Error(ErrorLevel.Error, "download-failed", $"Download '{url}' failed: {message}");

        public static Error YamlHeaderNotObject(bool isArray)
            => new Error(ErrorLevel.Warning, "yaml-header-not-object", $"Expect yaml header to be an object, but got {(isArray ? "an array" : "a scalar")}");

        public static Error YamlSyntaxError(YamlException ex)
            => new Error(ErrorLevel.Error, "yaml-syntax-error", $"{ex.Message}. {ex.InnerException?.Message}");

        public static Error YamlDuplicateKey(YamlException ex)
        {
            var (range, message) = RedefineDuplicateKeyErrorMessage(ex);
            return new Error(ErrorLevel.Error, "yaml-duplicate-key", message, range: range);
        }

        public static Error InvalidYamlHeader(Document file, Exception ex)
            => new Error(ErrorLevel.Warning, "invalid-yaml-header", ex.Message, file.ToString());

        public static Error JsonSyntaxError(Exception ex)
            => new Error(ErrorLevel.Error, "json-syntax-error", ex.Message);

        public static Error LinkIsEmpty(Document relativeTo)
            => new Error(ErrorLevel.Info, "link-is-empty", "Link is empty", relativeTo.ToString());

        public static Error LinkOutOfScope(Document relativeTo, Document file, string href)
            => new Error(ErrorLevel.Warning, "link-out-of-scope", $"File '{file}' referenced by link '{href}' will not be build because it is not included in docfx.yml", relativeTo.ToString());

        public static Error LinkIsDependency(Document relativeTo, Document file, string href)
            => new Error(ErrorLevel.Warning, "link-is-dependency", $"File '{file}' referenced by link '{href}' will not be build because it is from a dependency docset", relativeTo.ToString());

        public static Error AbsoluteFilePath(Document relativeTo, string path)
            => new Error(ErrorLevel.Warning, "absolute-file-path", $"File path cannot be absolute: '{path}'", relativeTo.ToString());

        public static Error HeadingNotFound(Document file)
            => new Error(ErrorLevel.Warning, "heading-not-found", $"The first visible block is not a heading block with `#`", file.ToString());

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

        public static Error BookmarkNotFound(Document relativeTo, Document reference, string bookmark)
            => new Error(ErrorLevel.Warning, "bookmark-not-found", $"Cannot find bookmark '#{bookmark}' in '{reference}'", relativeTo.ToString());

        public static Error NullValue(Range range, string name)
            => new Error(ErrorLevel.Info, "null-value", $"'{name}' contains null value", range: range);

        public static Error UnknownField(Range range, string propName, string typeName, string path)
            => new Error(ErrorLevel.Warning, "unknown-field", $"Path:{path} Could not find member '{propName}' on object of type '{typeName}'", range: range);

        public static Error ViolateSchema(Range range, string message)
            => new Error(ErrorLevel.Error, "violate-schema", message, range: range);

        public static Error SchemaNotFound(string schema)
            => new Error(ErrorLevel.Error, "schema-not-found", $"Unknown schema '{schema}'");

        private static Range ParseRangeFromYamlSyntaxException(YamlException ex)
        {
            return new Range(ex.Start.Line, ex.Start.Column, ex.End.Line, ex.End.Column);
        }

        private static (Range, string) RedefineDuplicateKeyErrorMessage(YamlException ex)
        {
            var range = ParseRangeFromYamlSyntaxException(ex);
            var innerMessage = ex.InnerException.Message;
            var keyIndex = innerMessage.LastIndexOf(':');
            var key = innerMessage.Substring(keyIndex + 2, innerMessage.Length - keyIndex - 2);
            return (range, $"Key '{key}' is already defined, remove the duplicate key.");
        }
    }
}
