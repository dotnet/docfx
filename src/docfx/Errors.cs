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

        public static Error CircularReference<T>(T filePath, IEnumerable<T> dependencyChain)
            => new Error(ErrorLevel.Error, "circular-reference", $"Found circular reference: {string.Join(" --> ", dependencyChain.Select(file => $"'{file}'"))} --> '{filePath}'", filePath.ToString());

        public static Error InvalidUserProfileCache(string userProfileCache, Exception ex)
            => new Error(ErrorLevel.Error, "invalid-user-profile-cache", ex.Message, userProfileCache);

        public static Error InvalidGitCommitsTime(string gitCommitsTimePath, Exception ex)
            => new Error(ErrorLevel.Error, "invalid-git-commits-time", ex.Message, gitCommitsTimePath);

        public static Error NeedRestore(string dependenyRepoHref)
            => new Error(ErrorLevel.Error, "need-restore", $"Cannot find dependency '{dependenyRepoHref}', did you forget to run `docfx restore`?");

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

        public static Error MissingTocHead(in Range range, string filePath)
            => new Error(ErrorLevel.Error, "missing-toc-head", $"The toc head name is missing", filePath, range);

        public static Error InvalidTocSyntax(in Range range, string filePath, string syntax)
            => new Error(ErrorLevel.Error, "invalid-toc-syntax", $"The toc syntax '{syntax}' is invalided", filePath, range);

        public static Error InvalidTocLevel(string filePath, int from, int to)
            => new Error(ErrorLevel.Error, "invalid-toc-level", $"The toc level can't be skipped from {from} to {to}", filePath);

        public static Error InvalidLocale(string locale)
            => new Error(ErrorLevel.Error, "invalid-locale", $"Locale '{locale}' is not supported.");

        public static Error DownloadFailed(string url, string message)
            => new Error(ErrorLevel.Error, "download-failed", $"Download '{url}' failed: {message}");

        public static Error GitCloneFailed(string url)
            => new Error(ErrorLevel.Error, "git-clone-failed", $"Cloning git repository '{url}' failed.");

        public static Error YamlHeaderNotObject(bool isArray)
            => new Error(ErrorLevel.Warning, "yaml-header-not-object", $"Expect yaml header to be an object, but got {(isArray ? "an array" : "a scalar")}");

        public static Error YamlSyntaxError(in Range range, string message)
            => new Error(ErrorLevel.Error, "yaml-syntax-error", message, range: range);

        public static Error YamlDuplicateKey(in Range range, string key)
            => new Error(ErrorLevel.Error, "yaml-duplicate-key", $"Key '{key}' is already defined, remove the duplicate key.", range: range);

        public static Error InvalidYamlHeader(Document file, Exception ex)
            => new Error(ErrorLevel.Warning, "invalid-yaml-header", ex.Message, file.ToString());

        public static Error JsonSyntaxError(in Range range, string message, string path)
            => new Error(ErrorLevel.Error, "json-syntax-error", $"{message}", range: range, jsonPath: path);

        public static Error LinkIsEmpty(Document relativeTo)
            => new Error(ErrorLevel.Info, "link-is-empty", "Link is empty", relativeTo.ToString());

        public static Error LinkOutOfScope(Document relativeTo, Document file, string href)
            => new Error(ErrorLevel.Warning, "link-out-of-scope", $"File '{file}' referenced by link '{href}' will not be built because it is not included in docfx.yml", relativeTo.ToString());

        public static Error RedirectionOutOfScope(Document redirection)
            => new Error(ErrorLevel.Warning, "redirection-out-of-scope", $"Redirection file '{redirection}' will not be built because it is not included in docfx.yml");

        public static Error LinkIsDependency(Document relativeTo, Document file, string href)
            => new Error(ErrorLevel.Warning, "link-is-dependency", $"File '{file}' referenced by link '{href}' will not be built because it is from a dependency docset", relativeTo.ToString());

        public static Error AbsoluteFilePath(Document relativeTo, string path)
            => new Error(ErrorLevel.Warning, "absolute-file-path", $"File path cannot be absolute: '{path}'", relativeTo.ToString());

        public static Error HeadingNotFound(Document file)
            => new Error(ErrorLevel.Warning, "heading-not-found", $"The first visible block is not a heading block with `#`", file.ToString());

        public static Error FileNotFound(string relativeTo, string path)
            => new Error(ErrorLevel.Warning, "file-not-found", $"Cannot find file '{path}' relative to '{relativeTo}'", relativeTo);

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

        public static Error BookmarkNotFound(Document relativeTo, Document reference, string bookmark, IEnumerable<string> candidateBookmarks)
            => new Error(ErrorLevel.Warning, "bookmark-not-found", $"Cannot find bookmark '#{bookmark}' in '{reference}'{(FindBestMatch(bookmark, candidateBookmarks, out string matchedBookmark) ? $", did you mean '#{matchedBookmark}'?" : null)}", relativeTo.ToString());

        public static Error NullValue(in Range range, string name, string path)
            => new Error(ErrorLevel.Info, "null-value", $"'{name}' contains null value", range: range, jsonPath: path);

        public static Error UnknownField(in Range range, string propName, string typeName, string path)
            => new Error(ErrorLevel.Warning, "unknown-field", $"Could not find member '{propName}' on object of type '{typeName}'.", range: range, jsonPath: path);

        public static Error ViolateSchema(in Range range, string message, string path)
            => new Error(ErrorLevel.Error, "violate-schema", $"{message}", range: range, jsonPath: path);

        public static Error SchemaNotFound(string schema)
            => new Error(ErrorLevel.Error, "schema-not-found", $"Unknown schema '{schema}'");

        public static Error ExceedMaxErrors(int maxErrors)
            => new Error(ErrorLevel.Error, "exceed-max-errors", $"Error or warning count exceed '{maxErrors}'. Build will continue but newer logs will be ignored.");

        public static Error UidConflict(string uid, IEnumerable<XrefSpec> conflicts)
        {
            var hint = conflicts.Count() > 5 ? "(Only 5 duplicates displayed)" : "";
            return new Error(ErrorLevel.Warning, "uid-conflict", $"Two or more documents have defined the same Uid '{uid}': {string.Join(',', conflicts.Select(spec => spec.Href).Take(5))}{hint}");
        }

        /// <summary>
        /// Find the string that best matches <paramref name="target"/> from <paramref name="candidates"/>,
        /// return if a match is found and assigned the found value to  <paramref name="bestMatch"/> accordingly. <para/>
        /// Returns: false if no match is found, otherwise return true.
        /// </summary>
        /// <param name="target">The string to be looked for</param>
        /// <param name="candidates">Possible strings to look for from</param>
        /// <param name="bestMatch">If a match is found, this will be assigned</param>
        /// <param name="threshold">Max levenshtein distance between the candidate and the target, greater values will be filtered</param>
        /// <returns>
        ///     if a match is found, return true and assign it to <paramref name="bestMatch"/>, otherwise return false.
        /// </returns>
        private static bool FindBestMatch(string target, IEnumerable<string> candidates, out string bestMatch, int threshold = 5)
        {
            bestMatch = candidates != null ?
                    (from candidate in candidates
                     let levanshteinDistance = Levenshtein.GetLevenshteinDistance(candidate, target)
                     where levanshteinDistance <= threshold
                     orderby levanshteinDistance, candidate
                     select candidate).FirstOrDefault()
                    : null;
            return !string.IsNullOrEmpty(bestMatch);
        }
    }
}
