// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    [SuppressMessage("Layout", "MEN002", Justification = "Suppress MEN002 for Errors.cs")]
    internal static class Errors
    {
        /// <summary>
        /// Build an OPS repo with a docset name that isn't provisioned.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error DocsetNotProvisioned(string name)
            => new Error(ErrorLevel.Warning, "docset-not-provisioned", $"Cannot build docset '{name}' because it isn't provisioned. Please go to Docs Portal (https://ops.microsoft.com) to provision first.");

        /// <summary>
        /// Build an OPS repo when the validation service goes down.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error ValidationIncomplete()
            => new Error(ErrorLevel.Warning, "validation-incomplete", $"Failed to get the validation ruleset and validation was not completed. This happens when there's an issue with the service and continuing to retry the call could cause build delays. You might have content issues that were not reported. To retry validation, close and re-open your PR, or rebuild your branch via Docs Portal (requires admin permissions). If you need admin help or if you continue to see this message, file an issue via https://SiteHelp.");

        /// <summary>
        /// Defined same redirection entry in both <see cref="Config.Redirections"/> and <see cref="Config.RedirectionsWithoutId"/>.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error RedirectionConflict(SourceInfo? source, string path)
            => new Error(ErrorLevel.Error, "redirection-conflict", $"The '{path}' appears twice or more in the redirection mappings", source);

        /// <summary>
        /// Redirection entry isn't a conceptual article(*.{md,json,yml}).
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error RedirectionInvalid(SourceInfo<string> source, string path)
            => new Error(ErrorLevel.Error, "redirection-invalid", $"File '{path}' is redirected to '{source}'. Only content files can be redirected.", source);

        /// <summary>
        /// The key or value of redirection is null or empty.
        /// </summary>
        public static Error RedirectionIsNullOrEmpty(SourceInfo<string> source, string from)
            => new Error(ErrorLevel.Error, "redirection-is-empty", $"The key or value of redirection '{from}: {source}' is null or empty", source);

        /// <summary>
        /// Multiple files defined in <see cref="Config.Redirections"/> are redirected to the same url,
        /// can't decide which entry to use when computing document id.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error RedirectionUrlConflict(SourceInfo<string> source)
            => new Error(ErrorLevel.Warning, "redirection-url-conflict", $"The '{source}' appears twice or more in the redirection mappings", source);

        /// <summary>
        /// The dest to redirection url does not match any files's publish URL, but the redirect_with_id flag has been set as true
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error RedirectionUrlNotFound(string from, SourceInfo<string> source)
        {
            var message = $"Can't preserve document ID for redirected file '{from}' " +
                          $"because redirect URL '{source}' is invalid or is a relative path to a file in another repo. " +
                          "Replace the redirect URL with a valid absolute URL, or set redirect_document_id to false in .openpublishing.redirection.json.";
            return new Error(ErrorLevel.Suggestion, "redirection-url-not-found", message, source);
        }

        /// <summary>
        /// Used invalid glob pattern in configuration.
        /// Examples:
        ///   - in build scope include/exclude files
        ///   - in file metadata glob
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error GlobPatternInvalid(string pattern, Exception ex)
            => new Error(ErrorLevel.Error, "glob-pattern-invalid", $"Glob pattern '{pattern}' is invalid: {ex.Message}");

        /// <summary>
        /// Build failure caused by English content when building localized docset.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error FallbackError(string defaultLocale)
            => new Error(ErrorLevel.Error, "fallback-error", $"Error(s) from '{defaultLocale}' repository caused this build failure, please check '{defaultLocale}' build report");

        /// <summary>
        /// Docfx.yml/docfx.json doesn't exist at the repo root.
        /// Examples:
        ///   - non-loc build, docfx.yml/docfx.json doesn't exist at the repo root
        ///   - loc build, docfx.yml/docfx.json doesn't exist at the neither loc repo root
        ///     nor source repo root
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error ConfigNotFound(string docsetPath)
            => new Error(ErrorLevel.Error, "config-not-found", $"Can't find docfx config file in '{docsetPath}'");

        /// <summary>
        /// Two files include each other.
        /// Examples:
        ///   - a.md includes b.md and reversely
        ///   - toc1 references toc2 and reversely
        ///   - toc references itself
        ///   - a.md references b.json's property with xref syntax, and b.json includes a.md reversely
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error CircularReference<T>(IEnumerable<T> dependencyChain, Document declaringFile)
            => new Error(ErrorLevel.Error, "circular-reference", $"Build has identified file(s) referencing each other: {string.Join(" --> ", dependencyChain.Select(file => $"'{file}'"))}", file: declaringFile.FilePath);

        /// <summary>
        /// Didn't run `docfx restore` before running `docfx build`.
        /// Examples:
        /// - can't find a cache(build required) file defined with url in config file
        /// - can't find dependent repo in file system
        /// </summary>
        /// Behavior: ❌ Message: ❌
        public static Error NeedRestore(string dependenyRepoHref)
            => new Error(ErrorLevel.Error, "need-restore", $"Cannot find dependency '{dependenyRepoHref}', did you forget to run `docfx restore`?");

        /// <summary>
        /// Failed to get a user from neither user cache nor github api by login.
        /// Examples:
        ///   - defined a non-existent author
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error AuthorNotFound(SourceInfo<string> login)
            => new Error(ErrorLevel.Warning, "author-not-found", $"Invalid value for author: '{login}' is not a valid GitHub ID", login);

        /// <summary>
        /// Failed to call a github api, e.g. GET /users/login.
        /// Examples:
        ///   - the api call reach github limit
        ///   - using invalid access token(more detailed info in ex.Message)
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error GitHubApiFailed(string message)
            => new Error(ErrorLevel.Warning, "github-api-failed", $"Call to GitHub API failed '{message}'. Try closing and reopening the PR. If you get this Error again, file an issue.");

        /// <summary>
        /// In yaml-format toc, topicHref SHOULD reference an article,
        /// rather than relative path or another toc file.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error InvalidTopicHref(SourceInfo<string?> source)
            => new Error(ErrorLevel.Error, "invalid-topic-href", $"The topic href '{source}' can only reference to a local file or absolute path", source);

        /// <summary>
        /// In markdown-format toc, link(treated as inclusion) CAN ONLY be toc file, folder or absolute path.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error InvalidTocHref(SourceInfo<string?> source)
            => new Error(ErrorLevel.Error, "invalid-toc-href", $"The toc href '{source}' can only reference to a local TOC file, folder or absolute path", source);

        /// <summary>
        /// In markdown-format toc, defined an empty node(# ) with no content.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error MissingTocHead(SourceInfo? source)
            => new Error(ErrorLevel.Warning, "missing-toc-head", $"The toc head name is missing", source);

        /// <summary>
        /// In markdown-format toc, used wrong toc syntax.
        /// Examples:
        ///   - The toc syntax '[bad1]()\n#[bad2](test.md)' is invalid,
        ///     the opening sequence of, characters must be followed by a space or by the end of line
        ///   - The toc syntax '# @b abc' is invalid, multiple inlines in one heading block is not allowed
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error InvalidTocSyntax(SourceInfo? source)
            => new Error(ErrorLevel.Error, "invalid-toc-syntax", $"The toc syntax is invalid, each line must be a valid markdown [ATX heading](https://spec.commonmark.org/0.28/#atx-heading) with a single link, xref link or literal text", source);

        /// <summary>
        /// In markdown-format toc, header level should be continuous, it shouldn't skip a level.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error InvalidTocLevel(SourceInfo? source, int from, int to)
            => new Error(ErrorLevel.Error, "invalid-toc-level", $"The toc level can't be skipped from {from} to {to}", source);

        /// <summary>
        /// Used invalid locale name(can't be resolved by <see cref="System.Globalization.CultureInfo"/>).
        /// </summary>
        public static Error LocaleInvalid(string locale)
            => new Error(ErrorLevel.Error, "locale-invalid", $"Invalid locale: '{locale}'.");

        /// <summary>
        /// Failed to download any file defined with url.
        /// Examples:
        ///   - failed to download for bad url
        ///   - failed to download due to bad network
        ///   - when update user profile cache fails, need to download verify etag
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error DownloadFailed(string url)
            => new Error(ErrorLevel.Error, "download-failed", $"Download failed for file '{url}'. Try closing and reopening the PR. If you get this Error again, file an issue.");

        /// <summary>
        /// Failed to run `git fetch` or `git worktree add`.
        /// Examples:
        ///   - restore a repo with bad url
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error GitCloneFailed(string url, string branch)
        {
            var message = $"Failure to clone the repository `{url}#{branch}`."
                      + "This could be caused by an incorrect repository URL, please verify the URL on the Docs Portal (https://ops.microsoft.com)."
                      + "This could also be caused by not having the proper permission the repository, "
                      + "please confirm that the GitHub group/team that triggered the build has access to the repository.";
            return new Error(ErrorLevel.Error, "git-clone-failed", message);
        }

        /// <summary>
        /// Yaml header defined in article.md isn't an object.
        /// The line should always be 2 since the file should always start with "---"
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error YamlHeaderNotObject(bool isArray, FilePath file)
            => new Error(ErrorLevel.Warning, "yaml-header-not-object", $"Expect yaml header to be an object, but got {(isArray ? "an array" : "a scalar")}", new SourceInfo(file, 2, 1));

        /// <summary>
        /// Syntax error in yaml file(not duplicate key).
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error YamlSyntaxError(SourceInfo? source, string message)
            => new Error(ErrorLevel.Error, "yaml-syntax-error", message, source);

        /// <summary>
        /// Syntax error in yaml header(not duplicate key).
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error YamlHeaderSyntaxError(Error error)
            => new Error(ErrorLevel.Warning, "yaml-header-syntax-error", error.Message, error.FilePath, error.Line, error.Column, error.EndLine, error.EndColumn);

        /// <summary>
        /// Used duplicate yaml key in markdown yml header or schema document(yml).
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error YamlDuplicateKey(SourceInfo? source, string key)
            => new Error(ErrorLevel.Suggestion, "yaml-duplicate-key", $"Key '{key}' is already defined, remove the duplicate key. NOTE: This Suggestion will become a Warning on 06/30/2020.", source);

        /// <summary>
        /// Syntax error in json file.
        /// Examples:
        ///   - unclosed ([{
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error JsonSyntaxError(SourceInfo? source, string message)
            => new Error(ErrorLevel.Error, "json-syntax-error", message, source);

        /// <summary>
        /// Link which's resolved to a file out of build scope.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error LinkOutOfScope(SourceInfo<string> source, Document file)
            => new Error(ErrorLevel.Warning, "link-out-of-scope", $"File '{file}' referenced by link '{source}' will not be built because it is not included in build scope", source);

        /// <summary>
        /// Used a link pointing to an rooted absolute file path.
        /// Examples:
        ///   - [Absolute](C:/a.md)
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error LocalFilePath(SourceInfo<string> path)
            => new Error(ErrorLevel.Warning, "local-file-path", $"Link '{path}' points to a local file. Use a relative path instead", path);

        /// <summary>
        /// The first tag in an article.md isn't h1 tag.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error HeadingNotFound(Document file)
            => new Error(ErrorLevel.Off, "heading-not-found", $"The first visible block is not a heading block with `#`, `##` or `###`", file.FilePath);

        /// <summary>
        /// Can't find a file referenced by configuration, or user writes a non-existing link.
        /// Examples:
        ///   - define user_profile.json file in config, while the file doesn't exist
        ///   - href referencing a non-existing file
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error FileNotFound(SourceInfo<string> source)
            => new Error(ErrorLevel.Warning, "file-not-found", $"Invalid file link: '{source}'.", source);

        /// <summary>
        /// Can't find a folder.
        /// Examples: pointing template to a local folder that does not exist
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error DirectoryNotFound(SourceInfo<string> source)
            => new Error(ErrorLevel.Error, "directory-not-found", $"Invalid directory: '{source}'.", source);

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
        /// Behavior: ✔️ Message: ❌
        public static Error MergeConflict(SourceInfo? source)
            => new Error(ErrorLevel.Suggestion, "merge-conflict", "File contains merge conflict markers. NOTE: This Suggestion will become a Warning on 06/30/2020.", source);

        /// <summary>
        /// Failed to resolve uid defined by @ syntax.
        /// </summary>
        /// Behavior: ❌ Message: ✔️
        public static Error AtXrefNotFound(SourceInfo<string> source)
            => new Error(ErrorLevel.Off, "at-xref-not-found", $"Cross reference not found: '{source}'", source);

        /// <summary>
        /// Failed to resolve uid defined by [link](xref:uid) or <xref:uid> syntax.
        /// </summary>
        /// Behavior: ❌ Message: ✔️
        public static Error XrefNotFound(SourceInfo<string> source)
            => new Error(ErrorLevel.Warning, "xref-not-found", $"Cross reference not found: '{source}'", source);

        /// <summary>
        /// Files published to the same url have no monikers or share common monikers.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error PublishUrlConflict(string url, IReadOnlyDictionary<Document, IReadOnlyList<string>> files, List<string> conflictMonikers)
        {
            var nonVersion = conflictMonikers.Contains(PublishModelBuilder.NonVersion);
            var message = conflictMonikers.Count != 0 && !nonVersion ? $" of the same version({Join(conflictMonikers)})" : null;
            return new Error(
                ErrorLevel.Error,
                "publish-url-conflict",
                $"Two or more files{message} publish to the same url '{url}': {Join(files.Select(file => $"{file.Key}{(nonVersion ? null : $"<{Join(file.Value)}>")}"))}");
        }

        /// <summary>
        /// More than one files are resolved to the same output path.
        /// Examples:
        ///   - in <see cref="Config.Redirections"/> section, defined an entry key that's also a file in build scope
        ///   - different file extension with same filename, like `Toc.yml` and `Toc.md`
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error OutputPathConflict(string path, IEnumerable<Document> files)
            => new Error(ErrorLevel.Error, "output-path-conflict", $"Two or more files output to the same path '{path}': {Join(files)}");

        /// <summary>
        /// Used docfx output model property which are not defined in input model.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error AttributeReserved(SourceInfo? source, string name)
            => new Error(ErrorLevel.Warning, "attribute-reserved", $"Attribute {name} is reserved for use by Docs. Remove it from your file metadata.", source);

        /// <summary>
        /// Metadata value must be scalar or arrays of scalars.
        /// </summary>
        public static Error InvalidMetadataType(SourceInfo? source, string name)
            => new Error(ErrorLevel.Error, "invalid-metadata-type", $"Metadata '{name}' can only be a scalar value or string array", source);

        /// <summary>
        /// Failed to compute specific info of a commit.
        /// </summary>
        public static Error GitCloneIncomplete(string repoPath)
            => new Error(ErrorLevel.Error, "git-clone-incomplete", $"Git repository '{repoPath}' is an incomplete clone, GitHub contributor list may not be accurate.");

        /// <summary>
        /// Git.exe isn't installed.
        /// </summary>
        /// Behavior: ❌ Message: ✔️
        public static Error GitNotFound()
            => new Error(ErrorLevel.Error, "git-not-found", $"Git isn't installed on the target machine. Try closing and reopening the PR. If you get this Error again, file an issue.");

        /// <summary>
        /// Failed to invoke `git revparse`(resolve commit history of a file on a non-existent branch).
        /// Examples:
        ///   - resolve contributors or authors on a locale-sxs branch while the corresponding locale branch doesn't exist
        /// </summary>
        /// Behavior: ❌ Message: ✔️
        public static Error CommittishNotFound(string repo, string committish)
            => new Error(ErrorLevel.Error, "committish-not-found", $"Can't find branch, tag, or commit '{committish}' for repo {repo}.");

        /// <summary>
        /// Defined refrence with by #bookmark fragment within articles, which doesn't exist.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error BookmarkNotFound(SourceInfo? source, Document reference, string bookmark, IEnumerable<string> candidateBookmarks)
            => new Error(ErrorLevel.Warning, "bookmark-not-found", $"Cannot find bookmark '#{bookmark}' in '{reference}'{(FindBestMatch(bookmark, candidateBookmarks, out var matchedBookmark) ? $", did you mean '#{matchedBookmark}'?" : null)}", source);

        // Behavior: ✔️ Message: ❌
        public static Error NullArrayValue(SourceInfo? source, string name)
            => new Error(ErrorLevel.Warning, "null-array-value", $"'{name}' contains null value, the null value has been removed", source);

        /// <summary>
        /// Defined extra field(s) in input model in schema document(json, yml).
        /// </summary>
        /// Behavior: ❌ Message: ❌
        public static Error UnknownField(SourceInfo? source, string propName, string typeName)
            => new Error(ErrorLevel.Warning, "unknown-field", $"Could not find member '{propName}' on object of type '{typeName}'.", source);

        /// <summary>
        /// Schema document with violate content type/value against predefined models(not syntax error).
        /// </summary>
        /// Behavior: ❌ Message: ❌
        public static Error ViolateSchema(SourceInfo? source, string message)
            => new Error(ErrorLevel.Error, "violate-schema", message, source);

        /// <summary>
        /// The input value type does not match expected value type.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error UnexpectedType(SourceInfo? source, object expectedType, object actualType)
            => new Error(ErrorLevel.Warning, "unexpected-type", $"Expected type '{expectedType}' but got '{actualType}'", source);

        /// <summary>
        /// The input value is not defined in a valid value list.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error InvalidValue(SourceInfo? source, string name, object value)
            => new Error(ErrorLevel.Warning, "invalid-value", $"Invalid value for '{name}': '{value}'", source);

        /// <summary>
        /// The string type's value doesn't match given format.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error FormatInvalid(SourceInfo? source, string value, object type)
            => new Error(ErrorLevel.Warning, "format-invalid", $"String '{value}' is not a valid '{type}'", source);

        /// <summary>
        /// Array length not within min and max.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error ArrayLengthInvalid(SourceInfo? source, string propName, string criteria)
            => new Error(ErrorLevel.Warning, "array-length-invalid", $"Array '{propName}' length should be {criteria}", source);

        /// <summary>
        /// Array items not unique.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error ArrayNotUnique(SourceInfo? source, string propName)
            => new Error(ErrorLevel.Warning, "array-not-unique", $"Array '{propName}' items should be unique", source);

        /// <summary>
        /// Array items not unique.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error ArrayContainsFailed(SourceInfo? source, string propName)
            => new Error(ErrorLevel.Warning, "array-contains-failed", $"Array '{propName}' should contain at least one item that matches JSON schema", source);

        /// <summary>
        /// Error when JSON boolean schema failed.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error BooleanSchemaFailed(SourceInfo? source, string propName)
            => new Error(ErrorLevel.Warning, "boolean-schema-failed", $"Boolean schema validation failed for '{propName}'", source);

        /// <summary>
        /// Object property count not within min and max.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error PropertyCountInvalid(SourceInfo? source, string propName, string criteria)
            => new Error(ErrorLevel.Warning, "property-count-invalid", $"Object '{propName}' property count should be {criteria}", source);

        /// <summary>
        /// String length not within min and max.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error StringLengthInvalid(SourceInfo? source, string propName, string criteria)
            => new Error(ErrorLevel.Warning, "string-length-invalid", $"String '{propName}' length should be {criteria}", source);

        /// <summary>
        /// Number not within min and max.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error NumberInvalid(SourceInfo? source, double value, string criteria)
            => new Error(ErrorLevel.Warning, "number-invalid", $"Number '{value}' should be {criteria}", source);

        /// <summary>
        /// A required attribute is missing.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error MissingAttribute(SourceInfo? source, string name)
            => new Error(ErrorLevel.Warning, "missing-attribute", $"Missing required attribute: '{name}'", source);

        /// <summary>
        /// An attribute lacks the required dependency.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error MissingPairedAttribute(SourceInfo? source, string name, string otherKey)
            => new Error(ErrorLevel.Warning, "missing-paired-attribute", $"Missing attribute: '{otherKey}'. If you specify '{name}', you must also specify '{otherKey}'", source);

        /// <summary>
        /// Attributes do not meet the requirements of either logic.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error MissingEitherAttribute(SourceInfo? source, IEnumerable<object> attributes)
            => new Error(ErrorLevel.Warning, "missing-either-attribute", $"One of the following attributes is required: {Join(attributes)}", source);

        /// <summary>
        /// Attributes do not meet the requirements of precludes logic.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error PrecludedAttributes(SourceInfo? source, IEnumerable<object> attributes)
            => new Error(ErrorLevel.Warning, "precluded-attributes", $"Only one of the following attributes can exist: {Join(attributes)}", source);

        /// <summary>
        /// An attribute does't conform to date format.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error DateFormatInvalid(SourceInfo? source, string name, string value)
            => new Error(ErrorLevel.Warning, "date-format-invalid", $"Invalid date format for '{name}': '{value}'.", source);

        /// <summary>
        /// Date out of range.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error DateOutOfRange(SourceInfo? source, string name, string value)
            => new Error(ErrorLevel.Warning, "date-out-of-range", $"Value out of range for '{name}': '{value}'", source);

        /// <summary>
        /// An attribute is deprecated.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error AttributeDeprecated(SourceInfo? source, string name, string replacedBy)
            => new Error(ErrorLevel.Warning, "attribute-deprecated", $"Deprecated attribute: '{name}'{(string.IsNullOrEmpty(replacedBy) ? "." : $", use '{replacedBy}' instead")}", source);

        /// <summary>
        /// The value of paired attribute is invalid.
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error InvalidPairedAttribute(SourceInfo? source, string name, object value, string dependentFieldName, object? dependentFieldValue)
            => new Error(ErrorLevel.Warning, "invalid-paired-attribute", $"Invalid value for '{name}': '{value}' is not valid with '{dependentFieldName}' value '{dependentFieldValue}'", source);

        /// <summary>
        /// The value is not a valid Microsoft alias
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error MsAliasInvalid(SourceInfo<string> alias, string name)
            => new Error(ErrorLevel.Warning, "ms-alias-invalid", $"Invalid value for '{name}', '{alias}' is not a valid Microsoft alias", alias);

        /// <summary>
        /// Call Microsoft Graph API failed
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error MicrosoftGraphApiFailed(string exMessage)
            => new Error(ErrorLevel.Warning, "microsoft-graph-api-failed", $"Call to Microsoft Graph API failed: {exMessage} Try closing and reopening the PR. If you get this Error again, file an issue.");

        /// <summary>
        /// Used unknown YamlMime.
        /// Examples:
        ///   - forgot to define schema in schema document(yml)
        ///   - defined a an unknown schema type(other than conceptual, contextObject, landingData)
        /// </summary>
        /// Behavior: ❌ Message: ✔️
        public static Error SchemaNotFound(SourceInfo<string> source)
            => new Error(ErrorLevel.Error, "schema-not-found", $"Unknown schema '{source}'", source);

        /// <summary>
        /// Build errors is larger than <see cref="OutputConfig.MaxErrors"/>.
        /// </summary>
        /// Behavior: ❌ Message: ❌
        public static Error ExceedMaxErrors(int maxErrors, ErrorLevel level)
            => new Error(level, "exceed-max-errors", $"{level} count exceed '{maxErrors}'. Build will continue but newer {level} logs will be ignored.");

        /// <summary>
        /// More than one files defined the same uid.
        /// Examples:
        ///   - both files with no monikers defined same uid
        /// </summary>
        /// Behavior: ✔️ Message: ✔️
        public static Error UidConflict(string uid, SourceInfo? source)
            => new Error(ErrorLevel.Warning, "uid-conflict", $"The same Uid '{uid}' has been defined multiple times in the same file", source);

        public static Error UidConflict(string uid, IEnumerable<FilePath> conflicts)
            => new Error(ErrorLevel.Warning, "uid-conflict", $"UID '{uid}' is defined in more than one file: {Join(conflicts)}");

        /// <summary>
        /// Same uid defined within different versions with the different name.
        /// Examples:
        ///   - Same uid defined in multiple .md files with different versions have different titles.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error UidPropertyConflict(string uid, string propertyName, IEnumerable<string> conflicts)
        {
            return new Error(ErrorLevel.Warning, "xref-property-conflict", $"UID '{uid}' is defined with different {propertyName}s: {Join(conflicts)}");
        }

        /// <summary>
        /// Multiple articles with same uid contain overlapped monikers,
        /// and can't decide which article to use when referencing that uid with this overlapped version
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error MonikerOverlapping(string uid, List<Document> files, IEnumerable<string> overlappingmonikers)
            => new Error(ErrorLevel.Error, "moniker-overlapping", $"Two or more documents with the same uid `{uid}`({Join(files)}) have defined overlapping moniker: {Join(overlappingmonikers)}");

        /// <summary>
        /// Failed to parse moniker string.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error MonikerRangeInvalid(SourceInfo<string> source, Exception ex)
            => new Error(ErrorLevel.Error, "moniker-range-invalid", $"Invalid moniker range: '{source}': {ex.Message}", source);

        /// <summary>
        /// MonikerRange is not defined in docfx.yml or doesn't match an article.md,
        /// which used monikerRange in its yaml header or used moniker-zone syntax.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error MonikerRangeUndefined(SourceInfo? source)
            => new Error(ErrorLevel.Suggestion, "moniker-range-undefined", "Moniker range missing in docfx.yml/docfx.json, user should not define it in file metadata or moniker zone. NOTE: This Suggestion will become a Error on 06/30/2020.", source);

        /// <summary>
        /// Config's monikerRange and monikerRange defined in yaml header has no intersection,
        /// or moniker-zone defined in article.md has no intersection with file-level monikers.
        /// </summary>
        /// Behavior: ✔️ Message: ❌
        public static Error MonikeRangeOutOfScope(SourceInfo<string> source, IReadOnlyList<string> zoneLevelMonikers, IReadOnlyList<string> fileLevelMonikers)
            => new Error(ErrorLevel.Error, "moniker-range-out-of-scope", $"No intersection between zone and file level monikers. The result of zone level range string '{source}' is {Join(zoneLevelMonikers)}, while file level monikers is {Join(fileLevelMonikers)}.", source);

        public static Error MonikeRangeOutOfScope(SourceInfo<string> configMonikerRange, IReadOnlyList<string> configMonikers, SourceInfo<string> monikerRange, IReadOnlyList<string> fileMonikers)
            => new Error(ErrorLevel.Error, "moniker-range-out-of-scope", $"No moniker intersection between docfx.yml/docfx.json and file metadata. Config moniker range '{configMonikerRange}' is {Join(configMonikers)}, while file moniker range '{monikerRange}' is {Join(fileMonikers)}", monikerRange);

        /// <summary>
        /// Custom 404 page is not supported
        /// Example:
        ///   - user want their 404.md to be built and shown as their 404 page of the website.
        /// </summary>
        public static Error Custom404Page(Document file)
            => new Error(ErrorLevel.Warning, "custom-404-page", $"Custom 404 page is not supported", file.FilePath);

        public static Error MetadataValidationRuleset(string ruleset)
            => new Error(ErrorLevel.Info, "MetadataValidationRuleset", $"Metadata validation ruleset used: {ruleset}");

        private static string Join<T>(IEnumerable<T> source)
        {
            var formatSource = source.Select(item => $"'{item}'").OrderBy(_ => _, StringComparer.Ordinal);
            return $"{string.Join(", ", formatSource.Take(5))}{(formatSource.Count() > 5 ? "..." : "")}";
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
        private static bool FindBestMatch(string target, IEnumerable<string> candidates, [NotNullWhen(true)] out string? bestMatch, int threshold = 5)
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
