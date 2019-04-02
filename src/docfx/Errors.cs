// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Errors
    {
        /// <summary>
        /// Defined same redirection entry in both <see cref="Config.Redirections"/> and <see cref="Config.RedirectionsWithoutId"/>.
        /// </summary>
        public static Error RedirectionConflict(string redirectFrom)
            => new Error(ErrorLevel.Error, "redirection-conflict", $"The '{redirectFrom}' appears twice or more in the redirection mappings");

        /// <summary>
        /// Redirection entry isn't a conceptual article(*.{md,json,yml}).
        /// </summary>
        public static Error InvalidRedirection(string path, ContentType contentType)
            => new Error(ErrorLevel.Error, "invalid-redirection", $"The '{path}' shouldn't belong to redirections since it's a {contentType}");

        /// <summary>
        /// The key or value of redirection is null or empty.
        /// </summary>
        public static Error RedirectionIsNullOrEmpty(string from, string to)
            => new Error(ErrorLevel.Error, "redirection-is-empty", $"The key or value of redirection '{from}: {to}' is null or empty");

        /// <summary>
        /// Defined redirect dest not starting with '\' in <see cref="Config.Redirections"/>.
        /// </summary>
        public static Error InvalidRedirectTo(string path, string redirectTo)
            => new Error(ErrorLevel.Warning, "invalid-redirect-to", $"The redirect dest '{redirectTo}' isn't allowed for entry '{path}' in redirections, redirect dest must start with '/'");

        /// <summary>
        /// Used invalid glob pattern in configuration.
        /// Examples:
        ///   - in build scope include/exclude files
        ///   - in file metadata glob
        /// </summary>
        public static Error InvalidGlobPattern(string pattern, Exception ex)
            => new Error(ErrorLevel.Error, "invalid-glob-pattern", $"The glob pattern '{pattern}' is invalid: {ex.Message}");

        /// <summary>
        /// Docfx.yml/docfx.json doesn't exist at the repo root.
        /// Examples:
        ///   - non-loc build, docfx.yml/docfx.json doesn't exist at the repo root
        ///   - loc build, docfx.yml/docfx.json doesn't exist at the neither loc repo root
        ///     nor source repo root
        /// </summary>
        public static Error ConfigNotFound(string docsetPath)
            => new Error(ErrorLevel.Error, "config-not-found", $"Cannot find 'docfx.yml/docfx.json' at '{docsetPath}'");

        /// <summary>
        /// Two files include each other.
        /// Examples:
        ///   - a.md includes b.md and reversely
        ///   - toc1 references toc2 and reversely
        ///   - toc references itself
        ///   - a.md references b.json's property with xref syntax, and b.json includes a.md reversely
        /// </summary>
        public static Error CircularReference<T>(IEnumerable<T> dependencyChain)
            => new Error(ErrorLevel.Error, "circular-reference", $"Found circular reference: {string.Join(" --> ", dependencyChain.Select(file => $"'{file}'"))}");

        /// <summary>
        /// Didn't run `docfx restore` before running `docfx build`.
        /// Examples:
        /// - can't find a cache(build required) file defined with url in config file
        /// - can't find dependent repo in file system
        /// </summary>
        public static Error NeedRestore(string dependenyRepoHref)
            => new Error(ErrorLevel.Error, "need-restore", $"Cannot find dependency '{dependenyRepoHref}', did you forget to run `docfx restore`?");

        /// <summary>
        /// Failed to get a user from neither user cache nor github api by login.
        /// Examples:
        ///   - defined a non-existent author
        /// </summary>
        public static Error GitHubUserNotFound(string login)
            => new Error(ErrorLevel.Warning, "github-user-not-found", $"Cannot find user '{login}' on GitHub");

        /// <summary>
        /// Failed to call a github api, e.g. GET /users/login.
        /// Examples:
        ///   - the api call reach github limit
        ///   - using invalid access token(more detailed info in ex.Message)
        /// </summary>
        public static Error GitHubApiFailed(string api, Exception ex)
            => new Error(ErrorLevel.Warning, "github-api-failed", $"Failed calling GitHub API '{api}': {ex.Message}");

        /// <summary>
        /// In yaml-format toc, topicHref SHOULD reference an article,
        /// rather than relative path or another toc file.
        /// </summary>
        public static Error InvalidTopicHref(Document relativeTo, string topicHref)
            => new Error(ErrorLevel.Error, "invalid-topic-href", $"The topic href '{topicHref}' can only reference to a local file or absolute path", relativeTo.ToString());

        /// <summary>
        /// In markdown-format toc, link(treated as inclusion) CAN ONLY be toc file, folder or absolute path.
        /// </summary>
        public static Error InvalidTocHref(Document relativeTo, string tocHref, Range range)
            => new Error(ErrorLevel.Error, "invalid-toc-href", $"The toc href '{tocHref}' can only reference to a local TOC file, folder or absolute path", relativeTo.ToString(), range);

        /// <summary>
        /// In markdown-format toc, defined an empty node(# ) with no content.
        /// </summary>
        public static Error MissingTocHead(in Range range, string filePath)
            => new Error(ErrorLevel.Error, "missing-toc-head", $"The toc head name is missing", filePath, range);

        /// <summary>
        /// In markdown-format toc, used wrong toc syntax.
        /// Examples:
        ///   - The toc syntax '[bad1]()\n#[bad2](test.md)' is invalid,
        ///     the opening sequence of, characters must be followed by a space or by the end of line
        ///   - The toc syntax '# @b abc' is invalid, multiple inlines in one heading block is not allowed
        /// </summary>
        public static Error InvalidTocSyntax(in Range range, string filePath, string syntax = null, string hint = null)
            => new Error(ErrorLevel.Error, "invalid-toc-syntax", $"The toc syntax '{syntax}' is invalid, {hint ?? "the opening sequence of # characters must be followed by a space or by the end of line"}. Refer to [ATX heading](https://spec.commonmark.org/0.28/#atx-heading) to fix it", filePath, range);

        /// <summary>
        /// In markdown-format toc, header level should be continuous, it shouldn't skip a level.
        /// </summary>
        public static Error InvalidTocLevel(string filePath, int from, int to)
            => new Error(ErrorLevel.Error, "invalid-toc-level", $"The toc level can't be skipped from {from} to {to}", filePath);

        /// <summary>
        /// Used invalid locale name(can't be resolved by <see cref="System.Globalization.CultureInfo"/>).
        /// </summary>
        public static Error InvalidLocale(string locale)
            => new Error(ErrorLevel.Error, "invalid-locale", $"Locale '{locale}' is not supported.");

        /// <summary>
        /// Failed to download any file defined with url.
        /// Examples:
        ///   - failed to download for bad url
        ///   - failed to download due to bad network
        ///   - when update user profile cache fails, need to download verify etag
        /// </summary>
        public static Error DownloadFailed(string url, string message)
            => new Error(ErrorLevel.Error, "download-failed", $"Download '{url}' failed: {message}");

        /// <summary>
        /// Failed to update user profile cache file.
        /// Examples:
        ///   - when <see cref="GitHubConfig.UpdateRemoteUserCache"/> is turned on, and docfx fails to
        ///     update the file cache with put request
        /// </summary>
        public static Error UploadFailed(string url, string message)
            => new Error(ErrorLevel.Warning, "upload-failed", $"Upload '{url}' failed: {message}");

        /// <summary>
        /// Failed to run `git fetch` or `git worktree add`.
        /// Examples:
        ///   - restore a repo with bad url
        /// </summary>
        public static Error GitCloneFailed(string url, IEnumerable<string> branches)
            => new Error(ErrorLevel.Error, "git-clone-failed", $"Cloning git repository '{url}' ({Join(branches)}) failed.");

        /// <summary>
        /// Yaml header defined in article.md isn't an object.
        /// </summary>
        public static Error YamlHeaderNotObject(bool isArray)
            => new Error(ErrorLevel.Warning, "yaml-header-not-object", $"Expect yaml header to be an object, but got {(isArray ? "an array" : "a scalar")}");

        /// <summary>
        /// Syntax error in yaml file(not duplicate key).
        /// </summary>
        public static Error YamlSyntaxError(in Range range, string message)
            => new Error(ErrorLevel.Error, "yaml-syntax-error", message, range: range);

        /// <summary>
        /// Used duplicate yaml key in markdown yml header or schema document(yml).
        /// </summary>
        public static Error YamlDuplicateKey(in Range range, string key)
            => new Error(ErrorLevel.Error, "yaml-duplicate-key", $"Key '{key}' is already defined, remove the duplicate key.", range: range);

        /// <summary>
        /// Syntax error in json file.
        /// Examples:
        ///   - unclosed ([{
        /// </summary>
        public static Error JsonSyntaxError(in Range range, string message, string path)
            => new Error(ErrorLevel.Error, "json-syntax-error", $"{message}", range: range, jsonPath: path);

        /// <summary>
        /// Used empty link in article.md.
        /// Examples:
        ///   - [link]()
        /// </summary>
        public static Error LinkIsEmpty(Document relativeTo)
            => new Error(ErrorLevel.Info, "link-is-empty", "Link is empty", relativeTo.ToString());

        /// <summary>
        /// Link which's resolved to a file out of build scope.
        /// </summary>
        public static Error LinkOutOfScope(Document relativeTo, Document file, string href, string configFile)
            => new Error(ErrorLevel.Warning, "link-out-of-scope", $"File '{file}' referenced by link '{href}' will not be built because it is not included in {configFile}", relativeTo.ToString());

        /// <summary>
        /// Defined a redirection entry that's not matched by config's files glob patterns.
        /// </summary>
        public static Error RedirectionOutOfScope(Document redirection, string configFile)
            => new Error(ErrorLevel.Info, "redirection-out-of-scope", $"Redirection file '{redirection}' will not be built because it is not included in {configFile}");

        /// <summary>
        /// Link which's resolved to a file in dependency repo won't be built.
        /// </summary>
        public static Error LinkIsDependency(Document relativeTo, Document file, string href)
            => new Error(ErrorLevel.Warning, "link-is-dependency", $"File '{file}' referenced by link '{href}' will not be built because it is from a dependency docset", relativeTo.ToString());

        /// <summary>
        /// Used a link pointing to an rooted absolute file path.
        /// Examples:
        ///   - [Absolute](C:/a.md)
        /// </summary>
        public static Error AbsoluteFilePath(Document relativeTo, string path)
            => new Error(ErrorLevel.Warning, "absolute-file-path", $"File path cannot be absolute: '{path}'", relativeTo.ToString());

        /// <summary>
        /// The fisrt tag in an article.md isn't h1 tag.
        /// </summary>
        public static Error HeadingNotFound(Document file)
            => new Error(ErrorLevel.Info, "heading-not-found", $"The first visible block is not a heading block with `#`, `##` or `###`", file.ToString());

        /// <summary>
        /// Can't find a file referenced by configuration, or user writes a non-existing link.
        /// Examples:
        ///   - define user_profile.json file in config, while the file doesn't exist
        ///   - href referencing a non-existing file
        /// </summary>
        public static Error FileNotFound(string relativeTo, string path, Range range)
            => new Error(ErrorLevel.Warning, "file-not-found", $"Cannot find file '{path}' relative to '{relativeTo}'", relativeTo, range);

        /// <summary>
        /// Failed to resolve uid defined by [link](xref:uid) or <xref:uid> syntax.
        /// </summary>
        public static Error UidNotFound(Document file, string uid, string rawXref)
            => new Error(ErrorLevel.Warning, "uid-not-found", $"Cannot find uid '{uid}' using xref '{rawXref}'", file.ToString());

        /// <summary>
        /// File contains git merge conflict.
        /// Examples:
        ///   - <![CDATA[
        ///     <<<<<<< HEAD
        ///     head content
        ///     =======
        ///     branch content
        ///     >>>>>>> refs/heads/branch
        /// ]]>
        /// </summary>
        public static Error MergeConflict(string file)
            => new Error(ErrorLevel.Error, "merge-conflict", "File contains merge conflict", file);

        /// <summary>
        /// Failed to resolve uid defined by @ syntax.
        /// </summary>
        public static Error AtUidNotFound(Document file, string uid, string rawXref)
            => new Error(ErrorLevel.Info, "at-uid-not-found", $"Cannot find uid '{uid}' using xref '{rawXref}'", file.ToString());

        /// <summary>
        /// Files published to the same url have no monikers or share common monikers.
        /// </summary>
        public static Error PublishUrlConflict(string url, IEnumerable<Document> files, IEnumerable<string> conflictMonikers)
        {
            var message = !conflictMonikers.Contains("NONE_VERSION") ? $" of the same version({Join(conflictMonikers)})" : null;
            return new Error(ErrorLevel.Error, "publish-url-conflict", $"Two or more files{message} publish to the same url '{url}': {Join(files, file => file.ContentType == ContentType.Redirection ? $"{file} <redirection>" : file.ToString())}");
        }

        /// <summary>
        /// More than one files are resolved to the same output path.
        /// Examples:
        ///   - in <see cref="Config.Redirections"/> section, defined an entry key that's also a file in build scope
        ///   - different file extension with same filename, like `Toc.yml` and `Toc.md`
        /// </summary>
        public static Error OutputPathConflict(string path, IEnumerable<Document> files)
            => new Error(ErrorLevel.Error, "output-path-conflict", $"Two or more files output to the same path '{path}': {Join(files, file => file.ContentType == ContentType.Redirection ? $"{file} <redirection>" : file.ToString())}");

        /// <summary>
        /// Multiple files defined in <see cref="Config.Redirections"/> are redirected to the same url,
        /// can't decide which entry to use when computing document id.
        /// </summary>
        public static Error RedirectionDocumentIdConflict(IEnumerable<Document> redirectFromDocs, string redirectTo)
            => new Error(ErrorLevel.Warning, "redirected-id-conflict", $"Multiple documents redirected to '{redirectTo}' with document id: {Join(redirectFromDocs)}");

        /// <summary>
        /// Used docfx output model property which are not defined in input model.
        /// </summary>
        public static Error ReservedMetadata(in Range range, string name, string removeFrom)
            => new Error(ErrorLevel.Warning, "reserved-metadata", $"Metadata '{name}' is reserved by docfx, remove this metadata: '{removeFrom}'", null, range);

        /// <summary>
        /// Failed to compute specific info of a commit.
        /// </summary>
        public static Error GitLogError(string repoPath, int errorCode)
            => new Error(ErrorLevel.Error, "git-log-error", $"Error computing git log [{errorCode}] for '{repoPath}', did you use a shallow clone?");

        /// <summary>
        /// Git.exe isn't installed.
        /// </summary>
        public static Error GitNotFound()
            => new Error(ErrorLevel.Error, "git-not-found", $"Cannot find git, install git https://git-scm.com/");

        /// <summary>
        /// Failed to invoke `git revparse`(resolve commit history of a file on a non-existent branch).
        /// Examples:
        ///   - resolve contributors or authors on a locale-sxs branch while the corresponding locale branch doesn't exist
        /// </summary>
        public static Error CommittishNotFound(string repo, string committish)
            => new Error(ErrorLevel.Error, "committish-not-found", $"Cannot find branch, tag or commit '{committish}' for repo '{repo}'.");

        /// <summary>
        /// Defined refrence with by #bookmark fragment between articles, which doesn't exist.
        /// </summary>
        public static Error ExternalBookmarkNotFound(Document relativeTo, Document reference, string bookmark, IEnumerable<string> candidateBookmarks, Range range)
            => new Error(ErrorLevel.Warning, "external-bookmark-not-found", $"Cannot find bookmark '#{bookmark}' in '{reference}'{(FindBestMatch(bookmark, candidateBookmarks, out string matchedBookmark) ? $", did you mean '#{matchedBookmark}'?" : null)}", relativeTo.ToString(), range);

        /// <summary>
        /// Defined refrence with by #bookmark fragment within articles, which doesn't exist.
        /// </summary>
        public static Error InternalBookmarkNotFound(Document file, string bookmark, IEnumerable<string> candidateBookmarks, Range range)
            => new Error(ErrorLevel.Suggestion, "internal-bookmark-not-found", $"Cannot find bookmark '#{bookmark}' in '{file}'{(FindBestMatch(bookmark, candidateBookmarks, out string matchedBookmark) ? $", did you mean '#{matchedBookmark}'?" : null)}", file.ToString(), range);

        /// <summary>
        /// Used null value in yaml header or schema documents.
        /// Examples:
        ///   - article.md has null-valued yaml header property
        ///   - toc.yml has node with null-value property
        /// </summary>
        public static Error NullValue(in Range range, string name, string path)
            => new Error(ErrorLevel.Info, "null-value", $"'{name}' contains null value", range: range, jsonPath: path);

        public static Error NullArrayValue(in Range range, string name, string path)
            => new Error(ErrorLevel.Warning, "null-array-value", $"'{name}' contains null value, the null value has been removed", range: range, jsonPath: path);

        /// <summary>
        /// Defined extra field(s) in input model in schema document(json, yml).
        /// </summary>
        public static Error UnknownField(in Range range, string propName, string typeName, string path)
            => new Error(ErrorLevel.Warning, "unknown-field", $"Could not find member '{propName}' on object of type '{typeName}'.", range: range, jsonPath: path);

        /// <summary>
        /// Schema document with violate content type/value against predefined models(not syntax error).
        /// </summary>
        public static Error ViolateSchema(in Range range, string message, string path)
            => new Error(ErrorLevel.Error, "violate-schema", $"{message}", range: range, jsonPath: path);

        /// <summary>
        /// Used unknown YamlMime.
        /// Examples:
        ///   - forgot to define schema in schema document(yml)
        ///   - defined a an unknown schema type(other than conceptual, contextObject, landingData)
        /// </summary>
        public static Error SchemaNotFound(string schema)
            => new Error(ErrorLevel.Error, "schema-not-found", !string.IsNullOrEmpty(schema) ? $"Unknown schema '{schema}', object model is missing." : $"Unknown schema '{schema}'");

        /// <summary>
        /// Build errors is larger than <see cref="OutputConfig.MaxErrors"/>.
        /// </summary>
        public static Error ExceedMaxErrors(int maxErrors, ErrorLevel level)
            => new Error(level, "exceed-max-errors", $"{level} count exceed '{maxErrors}'. Build will continue but newer {level} logs will be ignored.");

        /// <summary>
        /// More than one files defined the same uid.
        /// Examples:
        ///   - both files with no monikers defined same uid
        /// </summary>
        public static Error UidConflict(string uid, IEnumerable<string> conflicts = null)
        {
            if (conflicts is null)
            {
                return new Error(ErrorLevel.Error, "uid-conflict", $"The same Uid '{uid}' has been defined multiple times in the same file");
            }

            var hint = conflicts.Count() > 5 ? "(Only 5 duplicates displayed)" : "";
            return new Error(ErrorLevel.Error, "uid-conflict", $"Two or more documents have defined the same Uid '{uid}': {string.Join(',', conflicts.Take(5))}{hint}");
        }

        /// <summary>
        /// Multiple articles with same uid contain overlapped monikers,
        /// and can't decide which article to use when referencing that uid with this overlapped version
        /// </summary>
        public static Error MonikerOverlapping(IEnumerable<string> overlappingmonikers)
            => new Error(ErrorLevel.Error, "moniker-overlapping", $"Two or more documents have defined overlapping moniker: {Join(overlappingmonikers)}");

        /// <summary>
        /// Defined duplicate monikers in moniker definition file.
        /// </summary>
        public static Error MonikerNameConflict(string monikerName)
            => new Error(ErrorLevel.Error, "moniker-name-conflict", $"Two or more moniker definitions have the same monikerName `{monikerName}`");

        /// <summary>
        /// Failed to parse moniker string.
        /// </summary>
        public static Error InvalidMonikerRange(string monikerRange, string message)
            => new Error(ErrorLevel.Error, "invalid-moniker-range", $"MonikerRange `{monikerRange}` is invalid: {message}");

        /// <summary>
        /// MonikerRange is not defined in docfx.yml or doesn't match an article.md,
        /// which used monikerRange in its yaml header or used moniker-zone syntax.
        /// </summary>
        public static Error MonikerConfigMissing()
            => new Error(ErrorLevel.Warning, "moniker-config-missing", "Moniker range missing in docfx.yml/docfx.json, user should not define it in file metadata or moniker zone.");

        /// <summary>
        /// Config's monikerRange and monikerRange defined in yaml header has no intersection,
        /// or moniker-zone defined in article.md has no intersection with file-level monikers.
        /// </summary>
        public static Error EmptyMonikers(string message)
            => new Error(ErrorLevel.Warning, "empty-monikers", message);

        /// <summary>
        /// Referenced an article using uid with invalid moniker(?view=).
        /// Examples:
        ///   - article with uid `a` has only netcore-1.0 & netcore-1.1 version, but get referenced with @a?view=netcore-2.0
        /// </summary>
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
