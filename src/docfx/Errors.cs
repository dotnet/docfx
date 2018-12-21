// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal static class Errors
    {
        public static Error RedirectionConflict(string redirectFrom)
            => new Error(ErrorLevel.Error, "redirection-conflict", $"The '{redirectFrom}' appears twice or more in the redirection mappings");

        public static Error InvalidRedirection(string path, ContentType contentType)
            => new Error(ErrorLevel.Error, "invalid-redirection", $"The '{path}' shouldn't belong to redirections since it's a {contentType}");

        public static Error InvalidGlobPattern(string pattern, Exception ex)
            => new Error(ErrorLevel.Error, "invalid-glob-pattern", $"The glob pattern '{pattern}' is invalid: {ex.Message}");

        public static Error ConfigNotFound(string docsetPath, string configFile)
            => new Error(ErrorLevel.Error, "config-not-found", $"Cannot find {configFile} at '{docsetPath}'");

        public static Error CircularReference<T>(T filePath, IEnumerable<T> dependencyChain)
            => new Error(ErrorLevel.Error, "circular-reference", $"Found circular reference: {string.Join(" --> ", dependencyChain.Select(file => $"'{file}'"))} --> '{filePath}'", filePath.ToString());

        public static Error NeedRestore(string dependenyRepoHref)
            => new Error(ErrorLevel.Error, "need-restore", $"Cannot find dependency '{dependenyRepoHref}', did you forget to run `docfx restore`?");

        public static Error GitHubUserNotFound(string login)
            => new Error(ErrorLevel.Warning, "github-user-not-found", $"Cannot find user '{login}' on GitHub");

        public static Error GitHubApiFailed(string api, Exception ex)
            => new Error(ErrorLevel.Warning, "github-api-failed", $"Failed calling GitHub API '{api}': {ex.Message}");

        public static Error InvalidTopicHref(Document relativeTo, string topicHref)
            => new Error(ErrorLevel.Error, "invalid-topic-href", $"The topic href '{topicHref}' can only reference to a local file or absolute path", relativeTo.ToString());

        public static Error InvalidTocHref(Document relativeTo, string tocHref)
            => new Error(ErrorLevel.Error, "invalid-toc-href", $"The toc href '{tocHref}' can only reference to a local TOC file, folder or absolute path", relativeTo.ToString());

        public static Error MissingTocHead(in Range range, string filePath)
            => new Error(ErrorLevel.Error, "missing-toc-head", $"The toc head name is missing", filePath, range);

        public static Error InvalidTocSyntax(in Range range, string filePath, string syntax, string hint = null)
            => new Error(ErrorLevel.Error, "invalid-toc-syntax", $"The toc syntax '{syntax}' is invalid, {hint ?? "the opening sequence of # characters must be followed by a space or by the end of line"}. Refer to [ATX heading](https://spec.commonmark.org/0.28/#atx-heading) to fix it", filePath, range);

        public static Error InvalidTocLevel(string filePath, int from, int to)
            => new Error(ErrorLevel.Error, "invalid-toc-level", $"The toc level can't be skipped from {from} to {to}", filePath);

        public static Error InvalidLocale(string locale)
            => new Error(ErrorLevel.Error, "invalid-locale", $"Locale '{locale}' is not supported.");

        public static Error DownloadFailed(string url, string message)
            => new Error(ErrorLevel.Error, "download-failed", $"Download '{url}' failed: {message}");

        public static Error UploadFailed(string url, string message)
            => new Error(ErrorLevel.Warning, "upload-failed", $"Upload '{url}' failed: {message}");

        public static Error GitCloneFailed(string url, IEnumerable<string> branches)
            => new Error(ErrorLevel.Error, "git-clone-failed", $"Cloning git repository '{url}' ({Join(branches)}) failed.");

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

        public static Error LinkOutOfScope(Document relativeTo, Document file, string href, string configFile)
            => new Error(ErrorLevel.Warning, "link-out-of-scope", $"File '{file}' referenced by link '{href}' will not be built because it is not included in {configFile}", relativeTo.ToString());

        public static Error RedirectionOutOfScope(Document redirection, string configFile)
            => new Error(ErrorLevel.Warning, "redirection-out-of-scope", $"Redirection file '{redirection}' will not be built because it is not included in {configFile}");

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

        public static Error MergeConflict(string file)
            => new Error(ErrorLevel.Error, "merge-conflict", "File contains merge conflict", file);

        public static Error AtUidNotFound(Document file, string uid, string rawXref)
            => new Error(ErrorLevel.Info, "at-uid-not-found", $"Cannot find uid '{uid}' using xref '{rawXref}'", file.ToString());

        public static Error PublishUrlConflict(string url, IEnumerable<Document> files, IEnumerable<string> conflictMonikers)
        {
            var message = !conflictMonikers.Contains("NONE_VERSION") ? $" of the same version({Join(conflictMonikers)})" : null;
            return new Error(ErrorLevel.Error, "publish-url-conflict", $"Two or more files{message} publish to the same url '{url}': {Join(files, file => file.ContentType == ContentType.Redirection ? $"{file} <redirection>" : file.ToString())}");
        }

        public static Error IncludeRedirection(Document relativeTo, string path)
            => new Error(ErrorLevel.Warning, "include-is-redirection", $"Referenced inclusion {path} relative to '{relativeTo}' shouldn't belong to redirections", relativeTo.ToString());

        public static Error OutputPathConflict(string path, IEnumerable<Document> files)
            => new Error(ErrorLevel.Error, "output-path-conflict", $"Two or more files output to the same path '{path}': {Join(files, file => file.ContentType == ContentType.Redirection ? $"{file} <redirection>" : file.ToString())}");

        public static Error RedirectionDocumentIdConflict(IEnumerable<Document> redirectFromDocs, string redirectTo)
            => new Error(ErrorLevel.Warning, "redirected-id-conflict", $"Multiple documents redirected to '{redirectTo}' with document id: {Join(redirectFromDocs)}");

        public static Error ReservedMetadata(in Range range, string name, string removeFrom)
            => new Error(ErrorLevel.Error, "reserved-metadata", $"Metadata '{name}' is reserved by docfx, remove this metadata from {removeFrom}", null, range);

        public static Error GitLogError(string repoPath, int errorCode)
            => new Error(ErrorLevel.Error, "git-log-error", $"Error computing git log [{errorCode}] for '{repoPath}', did you used a shadow clone?");

        public static Error GitNotFound()
            => new Error(ErrorLevel.Error, "git-not-found", $"Cannot find git, install git https://git-scm.com/");

        public static Error CommittishNotFound(string repo, string committish)
            => new Error(ErrorLevel.Error, "committish-not-found", $"Cannot find branch, tag or commit '{committish}'for repo '{repo}'.");

        public static Error BookmarkNotFound(Document relativeTo, Document reference, string bookmark, IEnumerable<string> candidateBookmarks)
            => new Error(ErrorLevel.Warning, "bookmark-not-found", $"Cannot find bookmark '#{bookmark}' in '{reference}'{(FindBestMatch(bookmark, candidateBookmarks, out string matchedBookmark) ? $", did you mean '#{matchedBookmark}'?" : null)}", relativeTo.ToString());

        public static Error NullValue(in Range range, string name, string path)
            => new Error(ErrorLevel.Info, "null-value", $"'{name}' contains null value", range: range, jsonPath: path);

        public static Error UnknownField(in Range range, string propName, string typeName, string path)
            => new Error(ErrorLevel.Warning, "unknown-field", $"Could not find member '{propName}' on object of type '{typeName}'.", range: range, jsonPath: path);

        public static Error ViolateSchema(in Range range, string message, string path)
            => new Error(ErrorLevel.Error, "violate-schema", $"{message}", range: range, jsonPath: path);

        public static Error SchemaNotFound(string schema)
            => new Error(ErrorLevel.Error, "schema-not-found", !string.IsNullOrEmpty(schema) ? $"Unknown schema '{schema}', object model is missing." : $"Unknown schema '{schema}'");

        public static Error ExceedMaxErrors(int maxErrors)
            => new Error(ErrorLevel.Error, "exceed-max-errors", $"Error or warning count exceed '{maxErrors}'. Build will continue but newer logs will be ignored.");

        public static Error UidConflict(string uid, IEnumerable<XrefSpec> conflicts)
        {
            var hint = conflicts.Count() > 5 ? "(Only 5 duplicates displayed)" : "";
            return new Error(ErrorLevel.Error, "uid-conflict", $"Two or more documents have defined the same Uid '{uid}': {string.Join(',', conflicts.Select(spec => spec.Href).Take(5))}{hint}");
        }

        public static Error MonikerOverlapping(IEnumerable<string> overlappingmonikers)
            => new Error(ErrorLevel.Error, "moniker-overlapping", $"Two or more documents have defined overlapping moniker: {Join(overlappingmonikers)}");

        public static Error MonikerNameConflict(string monikerName)
            => new Error(ErrorLevel.Error, "moniker-name-conflict", $"Two or more moniker definitions have the same monikerName `{monikerName}`");

        public static Error InvalidMonikerRange(string monikerRange, string message)
            => new Error(ErrorLevel.Error, "invalid-moniker-range", $"MonikerRange `{monikerRange}` is invalid: {message}");

        public static Error MonikerConfigMissing()
            => new Error(ErrorLevel.Warning, "moniker-config-missing", "Moniker range missing in docfx.yml/docfx.json, user should not define it in file metadata or moniker zone.");

        public static Error EmptyMonikers(string message)
            => new Error(ErrorLevel.Warning, "empty-monikers", message);

        public static Error InvalidUidMoniker(string moniker, string uid)
            => new Error(ErrorLevel.Warning, "invalid-uid-moniker", $"Moniker '{moniker}' is not defined with uid '{uid}'");

        private static string Join<T>(IEnumerable<T> source, Func<T, string> selector = null)
            => string.Join(", ", source.Select(item => $"{selector?.Invoke(item) ?? item.ToString()}").OrderBy(_ => _, StringComparer.Ordinal).Select(_ => $"'{_}'").Take(5));

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
