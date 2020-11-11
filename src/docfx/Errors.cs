// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Docs.Build
{
    [SuppressMessage("Layout", "MEN002", Justification = "Suppress MEN002 for Errors.cs")]
    internal static class Errors
    {
        public static class System
        {
            /// <summary>
            /// Build an OPS repo when the validation service goes down.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error ValidationIncomplete()
                => new Error(ErrorLevel.Warning, "validation-incomplete", $"Failed to get the validation ruleset and validation was not completed. This happens when there's an issue with the service and continuing to retry the call could cause build delays. You might have content issues that were not reported. To retry validation, close and re-open your PR, or rebuild your branch via Docs Portal (requires admin permissions). If you need admin help or if you continue to see this message, file an issue via https://SiteHelp.");

            /// <summary>
            /// Didn't run `docfx restore` before running `docfx build`.
            /// Examples:
            /// - can't find a cache(build required) file defined with url in config file
            /// - can't find dependent repo in file system
            /// </summary>
            /// Behavior: ❌ Message: ❌
            public static Error NeedRestore(string dependencyRepoHref)
                => new Error(ErrorLevel.Error, "need-restore", $"Cannot find dependency '{dependencyRepoHref}', did you forget to run `docfx restore`?");

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
                => new Error(ErrorLevel.Error, "git-clone-failed", $"Failure to clone the repository `{url}#{branch}`. This could be caused by an incorrect repository URL, please verify the URL on the Docs Portal (https://ops.microsoft.com). This could also be caused by not having the proper permission the repository, please confirm that the GitHub group/team that triggered the build has access to the repository.");

            /// <summary>
            /// Failed to compute specific info of a commit.
            /// </summary>
            public static Error GitCloneIncomplete(string repoPath)
                => new Error(ErrorLevel.Warning, "git-clone-incomplete", $"Git repository '{repoPath}' is an incomplete clone, GitHub contributor list may not be accurate.");

            /// <summary>
            /// Git.exe isn't installed.
            /// </summary>
            /// Behavior: ❌ Message: ✔️
            public static Error GitNotFound()
                => new Error(ErrorLevel.Error, "git-not-found", $"Git isn't installed on the target machine. Try closing and reopening the PR. If you get this Error again, file an issue.");

            /// <summary>
            /// Call Microsoft Graph API failed
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error MicrosoftGraphApiFailed(string exMessage)
                => new Error(ErrorLevel.Warning, "microsoft-graph-api-failed", $"Call to Microsoft Graph API failed: {exMessage} Try closing and reopening the PR. If you get this Error again, file an issue.");

            public static Error MetadataValidationRuleset(string ruleset)
                => new Error(ErrorLevel.Info, "MetadataValidationRuleset", $"Metadata validation ruleset used: {ruleset}.");
        }

        public static class Logging
        {
            /// <summary>
            /// Build errors is larger than <see cref="OutputConfig.MaxFileErrors"/>.
            /// </summary>
            /// Behavior: ❌ Message: ❌
            public static Error ExceedMaxFileErrors(int maxErrors, ErrorLevel level, FilePath file)
                => new Error(ErrorLevel.Info, "exceed-max-file-errors", $"{level} count exceed '{maxErrors}'. Build will continue but newer logs in '{file}' will be ignored.", new SourceInfo(file));

            /// <summary>
            /// Build failure caused by English content when building localized docset.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error FallbackError(string defaultLocale)
                => new Error(ErrorLevel.Error, "fallback-error", $"Error(s) from '{defaultLocale}' repository caused this build failure, please check '{defaultLocale}' build report.");

            /// <summary>
            /// Validation rule is not overridable in docfx config.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error RuleOverrideInvalid(string code, SourceInfo? source)
                => new Error(ErrorLevel.Warning, "rule-override-invalid", $"Validation rule '{code}' is not overridable, so overrides in docfx.yml/docfx.json will be ignored.", source);
        }

        public static class Json
        {
            /// <summary>
            /// Syntax error in json file.
            /// Examples:
            ///   - unclosed ([{
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error JsonSyntaxError(SourceInfo? source, string message)
                => new Error(ErrorLevel.Error, "json-syntax-error", $"{message}", source);

            // Behavior: ✔️ Message: ❌
            public static Error NullArrayValue(SourceInfo? source, string name)
                => new Error(ErrorLevel.Warning, "null-array-value", $"'{name}' contains null value, the null value has been removed.", source);

            /// <summary>
            /// Schema document with violate content type/value against predefined models(not syntax error).
            /// </summary>
            /// Behavior: ❌ Message: ❌
            public static Error ViolateSchema(SourceInfo? source, string message)
                => new Error(ErrorLevel.Error, "violate-schema", $"{message}", source);
        }

        public static class Yaml
        {
            /// <summary>
            /// Yaml header defined in article.md isn't an object.
            /// The line should always be 2 since the file should always start with "---"
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error YamlHeaderNotObject(bool isArray, FilePath file)
                => new Error(ErrorLevel.Warning, "yaml-header-not-object", $"Expect yaml header to be an object, but got {(isArray ? "an array" : "a scalar")}.", new SourceInfo(file, 2, 1));

            /// <summary>
            /// Syntax error in yaml file(not duplicate key).
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error YamlSyntaxError(SourceInfo? source, string message)
                => new Error(ErrorLevel.Error, "yaml-syntax-error", $"{message}", source);

            /// <summary>
            /// Syntax error in yaml header(not duplicate key).
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error YamlHeaderSyntaxError(Error error)
                => new Error(ErrorLevel.Warning, "yaml-header-syntax-error", $"{error.Message}", error.Source);

            /// <summary>
            /// Used duplicate yaml key in markdown yml header or schema document(yml).
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error YamlDuplicateKey(SourceInfo? source, string key)
                => new Error(ErrorLevel.Warning, "yaml-duplicate-key", $"Key '{key}' is already defined, remove the duplicate key.", source);

            /// <summary>
            /// Used unknown YamlMime.
            /// Examples:
            ///   - forgot to define schema in schema document(yml)
            ///   - defined a an unknown schema type(other than conceptual, contextObject, landingData)
            /// </summary>
            /// Behavior: ❌ Message: ✔️
            public static Error SchemaNotFound(SourceInfo<string?> source)
                => new Error(ErrorLevel.Error, "schema-not-found", $"Unknown schema '{source}'.", source);
        }

        public static class Config
        {
            /// <summary>
            /// Docfx.yml/docfx.json doesn't exist at the repo root.
            /// Examples:
            ///   - non-loc build, docfx.yml/docfx.json doesn't exist at the repo root
            ///   - loc build, docfx.yml/docfx.json doesn't exist at the neither loc repo root
            ///     nor source repo root
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error ConfigNotFound(string docsetPath)
                => new Error(ErrorLevel.Error, "config-not-found", $"Can't find docfx config file in '{docsetPath}'.");

            /// <summary>
            /// Build an OPS repo with a docset name that isn't provisioned.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error DocsetNotProvisioned(string name)
                => new Error(ErrorLevel.Warning, "docset-not-provisioned", $"Cannot build docset '{name}' because it isn't provisioned. Please go to Docs Portal (https://ops.microsoft.com) to provision first.");

            /// <summary>
            /// Used invalid glob pattern in configuration.
            /// Examples:
            ///   - in build scope include/exclude files
            ///   - in file metadata glob
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error GlobPatternInvalid(string pattern, Exception ex)
                => new Error(ErrorLevel.Error, "glob-pattern-invalid", $"Glob pattern '{pattern}' is invalid: {ex.Message}.");

            /// <summary>
            /// Used invalid locale name(can't be resolved by <see cref="System.Globalization.CultureInfo"/>).
            /// </summary>
            public static Error LocaleInvalid(string locale)
                => new Error(ErrorLevel.Error, "locale-invalid", $"Invalid locale: '{locale}'.");

            /// <summary>
            /// Can't find a folder.
            /// Examples: pointing template to a local folder that does not exist
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error DirectoryNotFound(string directory)
                => new Error(ErrorLevel.Error, "directory-not-found", $"Invalid directory: '{directory}'.");

            /// <summary>
            /// Failed to invoke `git revparse`(resolve commit history of a file on a non-existent branch).
            /// Examples:
            ///   - resolve contributors or authors on a locale-sxs branch while the corresponding locale branch doesn't exist
            /// </summary>
            /// Behavior: ❌ Message: ✔️
            public static Error CommittishNotFound(string repo, string committish)
                => new Error(ErrorLevel.Error, "committish-not-found", $"Can't find branch, tag, or commit '{committish}' for repo {repo}.");
        }

        public static class Link
        {
            /// <summary>
            /// Link which is resolved to a file out of build scope.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error LinkOutOfScope(SourceInfo<string> source, FilePath file)
                => new Error(ErrorLevel.Warning, "link-out-of-scope", $"File '{file}' referenced by link '{source}' will not be built because it is not included in build scope.", source);

            /// <summary>
            /// Used a link pointing to an rooted absolute file path.
            /// Examples:
            ///   - [Absolute](C:/a.md)
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error LocalFilePath(SourceInfo<string> path)
                => new Error(ErrorLevel.Warning, "local-file-path", $"Link '{path}' points to a local file. Use a relative path instead.", path);

            /// <summary>
            /// Two files include each other.
            /// Examples:
            ///   - a.md includes b.md and reversely
            ///   - toc1 references toc2 and reversely
            ///   - toc references itself
            ///   - a.md references b.json's property with xref syntax, and b.json includes a.md reversely
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error CircularReference<T>(SourceInfo? source, T current, IEnumerable<T> recursionDetector, Func<T, string?>? display = null)
            {
                display ??= obj => obj?.ToString();
                var dependencyChain = string.Join(" --> ", recursionDetector.Reverse().Concat(new[] { current }).Select(file => $"'{display(file)}'"));
                return new Error(ErrorLevel.Error, "circular-reference", $"Build has identified file(s) referencing each other: {dependencyChain}.", source);
            }

            /// <summary>
            /// Can't find a file referenced by configuration, or user writes a non-existing link.
            /// Examples:
            ///   - define user_profile.json file in config, while the file doesn't exist
            ///   - href referencing a non-existing file
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error FileNotFound(SourceInfo<string> source)
                => new Error(ErrorLevel.Warning, "file-not-found", $"Invalid file link: '{source}'.", source);
        }

        public static class UrlPath
        {
            /// <summary>
            /// Files published to the same url have no monikers or share common monikers.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error PublishUrlConflict(string url, IReadOnlyDictionary<FilePath, MonikerList> files, List<string> conflictMonikers)
            {
                var message = conflictMonikers.Count != 0 ? $" of the same version({StringUtility.Join(conflictMonikers)})" : null;
                return new Error(
                    ErrorLevel.Warning,
                    "publish-url-conflict",
                    $"Two or more files{message} publish to the same url '{url}': {StringUtility.Join(files.Select(file => $"{file.Key}{(conflictMonikers.Count == 0 ? null : $"<{StringUtility.Join(file.Value)}>")}"))}.");
            }

            /// <summary>
            /// More than one files are resolved to the same output path.
            /// Examples:
            ///   - in <see cref="Config.Redirections"/> section, defined an entry key that's also a file in build scope
            ///   - different file extension with same filename, like `Toc.yml` and `Toc.md`
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error OutputPathConflict(string path, IEnumerable<FilePath> files)
                => new Error(ErrorLevel.Warning, "output-path-conflict", $"Two or more files output to the same path '{path}': {StringUtility.Join(files)}.");
        }

        public static class Redirection
        {
            /// <summary>
            /// Defined same redirection entry in both <see cref="Config.Redirections"/> and <see cref="Config.RedirectionsWithoutId"/>.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error RedirectionConflict(SourceInfo? source, string path)
                => new Error(ErrorLevel.Error, "redirection-conflict", $"The '{path}' appears twice or more in the redirection mappings.", source);

            /// <summary>
            /// Redirection entry isn't a conceptual article(*.{md,json,yml}).
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error RedirectionInvalid(SourceInfo<string> source, string path)
                => new Error(ErrorLevel.Error, "redirection-invalid", $"File '{path}' is redirected to '{source}'. Only content files can be redirected.", source);

            /// <summary>
            /// Multiple files defined in <see cref="Config.Redirections"/> are redirected to the same url,
            /// can't decide which entry to use when computing document id.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error RedirectionUrlConflict(SourceInfo<string> source)
                => new Error(ErrorLevel.Warning, "redirection-url-conflict", $"The '{source}' appears twice or more in the redirection mappings.", source);

            /// <summary>
            /// The dest to redirection url does not match any files's publish URL, but the redirect_with_id flag has been set as true
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error RedirectUrlInvalid(string from, SourceInfo<string> source)
                => new Error(ErrorLevel.Suggestion, "redirect-url-invalid", $"Can't redirect document ID for redirected file '{from}' because redirect URL '{source}' is invalid or is in a different docset. Specify a redirect_url in the same docset, or set redirect_document_id to false in .openpublishing.redirection.json.", source);

            public static Error CircularRedirection(SourceInfo? source, IEnumerable<FilePath> redirectionChain)
                => new Error(ErrorLevel.Warning, "circular-redirection", $"Build has identified circular redirection: {string.Join(" --> ", redirectionChain)}.", source);
        }

        public static class TableOfContents
        {
            /// <summary>
            /// In yaml-format toc, topicHref SHOULD reference an article,
            /// rather than relative path or another toc file.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error InvalidTopicHref(SourceInfo<string?> source)
                => new Error(ErrorLevel.Error, "invalid-topic-href", $"The topic href '{source}' can only reference to a local file or absolute path.", source);

            /// <summary>
            /// In markdown-format toc, link(treated as inclusion) CAN ONLY be toc file, folder or absolute path.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error InvalidTocHref(SourceInfo<string?> source)
                => new Error(ErrorLevel.Error, "invalid-toc-href", $"The toc href '{source}' can only reference to a local TOC file, folder or absolute path.", source);

            /// <summary>
            /// Toc inclusion with relative folder, no toc.{md,yml} file in corresponding folder.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error TocNotFound(SourceInfo<string> source)
                => new Error(ErrorLevel.Warning, "toc-not-found", $"Unable to find either toc.yml or toc.md inside {source} Please make sure the file exists.", source);

            /// <summary>
            /// In markdown-format toc, used wrong toc syntax.
            /// Examples:
            ///   - The toc syntax '[bad1]()\n#[bad2](test.md)' is invalid,
            ///     the opening sequence of, characters must be followed by a space or by the end of line
            ///   - The toc syntax '# @b abc' is invalid, multiple inlines in one heading block is not allowed
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error InvalidTocSyntax(SourceInfo? source)
                => new Error(ErrorLevel.Error, "invalid-toc-syntax", $"The toc syntax is invalid, each line must be a valid markdown [ATX heading](https://spec.commonmark.org/0.28/#atx-heading) with a single link, xref link or literal text.", source);

            /// <summary>
            /// In markdown-format toc, header level should be continuous, it shouldn't skip a level.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error InvalidTocLevel(SourceInfo? source, int from, int to)
                => new Error(ErrorLevel.Error, "invalid-toc-level", $"The toc level can't be skipped from {from} to {to}.", source);
        }

        public static class Xref
        {
            /// <summary>
            /// Failed to resolve uid defined by @ syntax.
            /// </summary>
            /// Behavior: ❌ Message: ✔️
            public static Error AtXrefNotFound(SourceInfo<string> source)
                => new Error(ErrorLevel.Off, "at-xref-not-found", $"Cross reference not found: '{source}'.", source);

            /// <summary>
            /// Failed to resolve uid defined by [link](xref:uid) or <xref:uid> syntax.
            /// </summary>
            /// Behavior: ❌ Message: ✔️
            public static Error XrefNotFound(SourceInfo<string> source)
                => new Error(ErrorLevel.Warning, "xref-not-found", $"Cross reference not found: '{source}'.", source);

            public static Error XrefTypeInvalid(SourceInfo<string> xref, string expectedXrefType, string? actualXrefType)
               => new Error(ErrorLevel.Warning, "xref-type-invalid", $"Invalid cross reference: '{xref}'. Expected type '{expectedXrefType}' but got '{actualXrefType}'.", xref);

            public static Error UidNotFound(string uid, IEnumerable<string?> repositories, string? schemaType)
                => new Error(ErrorLevel.Warning, "uid-not-found", $"UID '{uid}' with type '{schemaType}' not found, which is referenced by repository {StringUtility.Join(repositories)}.", null);

            /// <summary>
            /// The same uid of the same version is defined in multiple places
            /// Examples:
            ///   - both files with no monikers defined same uid
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error DuplicateUid(SourceInfo<string> uid, IEnumerable<SourceInfo> conflicts, string? propertyPath)
                => new Error(ErrorLevel.Warning, "duplicate-uid", $"UID '{uid}' is duplicated in {StringUtility.Join(conflicts)}.", uid, propertyPath);

            /// <summary>
            /// The same uid is defined in multiple docsets
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error DuplicateUidGlobal(SourceInfo<string> uid, string? repositoryUrl, string? propertyPath)
                => new Error(ErrorLevel.Warning, "duplicate-uid-global", $"UID '{uid}' is duplicated globally in repository '{repositoryUrl}'.", uid, propertyPath);

            /// <summary>
            /// Same uid defined within different versions with different values of the same xref property.
            /// Examples:
            ///   - Same uid defined in multiple .md files with different versions have different titles.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error XrefPropertyConflict(string uid, string propertyName, IEnumerable<string?> conflicts)
                => new Error(ErrorLevel.Info, "xref-property-conflict", $"UID '{uid}' is defined with different {propertyName}s: {StringUtility.Join(conflicts)}.");
        }

        public static class Versioning
        {
            public static Error DuplicateMonikerConfig(SourceInfo? source)
                => new Error(ErrorLevel.Warning, "duplicate-moniker-config", $"Both 'monikers' and 'monikerRange' are defined, 'monikers' is ignored.", source);

            /// <summary>
            /// Multiple articles with same uid contain overlapped monikers,
            /// and can't decide which article to use when referencing that uid with this overlapped version
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error MonikerOverlapping(string uid, List<FilePath> files, IEnumerable<string> overlappingMonikers)
                => new Error(ErrorLevel.Error, "moniker-overlapping", $"Two or more documents with the same uid `{uid}`({StringUtility.Join(files)}) have defined overlapping moniker: {StringUtility.Join(overlappingMonikers)}.");

            /// <summary>
            /// Failed to parse moniker string.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error MonikerRangeInvalid(SourceInfo? operand, FormattableString message)
                => new Error(ErrorLevel.Error, "moniker-range-invalid", message, operand);

            /// <summary>
            /// MonikerRange is not defined in docfx.yml or doesn't match an article.md,
            /// which used monikerRange in its yaml header or used moniker-zone syntax.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error MonikerRangeUndefined(SourceInfo? source)
                => new Error(ErrorLevel.Suggestion, "moniker-range-undefined", $"Moniker range missing in docfx.yml/docfx.json, user should not define it in file metadata or moniker zone. NOTE: This Suggestion will become a Error on 06/30/2020.", source);

            /// <summary>
            /// Moniker-zone defined in article.md has no intersection with file-level monikers.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error MonikerZoneEmpty(SourceInfo<string?> source, IEnumerable<string> zoneLevelMonikers, IEnumerable<string> fileLevelMonikers)
                => new Error(ErrorLevel.Warning, "moniker-zone-empty", $"No intersection between zone and file level monikers. The result of zone level range string '{source}' is {StringUtility.Join(zoneLevelMonikers)}, while file level monikers is {StringUtility.Join(fileLevelMonikers)}.", source);

            /// <summary>
            /// Config's monikerRange and monikerRange defined in yaml header has no intersection
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error MonikeRangeOutOfScope(SourceInfo<string?> configMonikerRange, IEnumerable<string> configMonikers, IEnumerable<string> fileMonikers, SourceInfo? source)
                => new Error(ErrorLevel.Error, "moniker-range-out-of-scope", $"No moniker intersection between docfx.yml/docfx.json and file metadata. Config moniker range '{configMonikerRange}' is {StringUtility.Join(configMonikers)}, while file monikers is {StringUtility.Join(fileMonikers)}.", source);
        }

        public static class ZonePivot
        {
            public static Error ZonePivotGroupDefinitionNotFound(FilePath file, string publishUrl)
                => new Error(ErrorLevel.Suggestion, "zone-pivot-definition-not-found", $"No source file is present for '{publishUrl}'. To use zone pivots, you must first define the zone pivot groups in zone-pivot-groups.yml or specify a file by 'zone_pivot_group_filename' metadata. Cross docset reference to zone pivot groups definition not supported by docfx.", new SourceInfo(file));

            public static Error ZonePivotGroupDefinitionConflict(FilePath file, string publishUrl)
                => new Error(ErrorLevel.Suggestion, "zone-pivot-definition-conflict", $"Multiple source files are present for '{publishUrl}'.", new SourceInfo(file));

            public static Error ZonePivotGroupNotFound(SourceInfo? source, string groupId, FilePath? definitionFile)
                => new Error(ErrorLevel.Suggestion, "pivot-group-not-found", $"Pivot group '{groupId}' isn't defined in '{definitionFile}'. Make sure every pivot group you reference in your content has been properly defined.", source);

            public static Error ZonePivotGroupNotSpecified(SourceInfo? source)
                => new Error(ErrorLevel.Suggestion, "zone-pivot-group-missing", $"Missing metadata attribute: zone_pivot_groups. To use zone pivots in your file, you must specify the valid zone pivot group that contains the pivot IDs you want to use.", source);
        }

        public static class Markdown
        {
            public static Error IncludeNotFound(SourceInfo<string?> source)
                => new Error(ErrorLevel.Error, "include-not-found", $"Invalid include link: '{source}'.", source);
        }

        public static class JsonSchema
        {
            /// <summary>
            /// Defined extra field(s) in input model in schema document(json, yml).
            /// </summary>
            /// Behavior: ❌ Message: ❌
            public static Error UnknownField(SourceInfo? source, string propName, string typeName)
                => new Error(ErrorLevel.Warning, "unknown-field", $"Could not find member '{propName}' on object of type '{typeName}'.", source, propName);

            /// <summary>
            /// The input value type does not match expected value type.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error UnexpectedType(SourceInfo? source, object expectedType, object actualType, string? name = null)
                => new Error(ErrorLevel.Warning, "unexpected-type", $"Expected type '{expectedType}' but got '{actualType}'.", source, name);

            /// <summary>
            /// The input value is not defined in a valid value list.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error InvalidValue(SourceInfo? source, string name, object value, string? propName = null)
                => new Error(ErrorLevel.Warning, "invalid-value", $"Invalid value for '{name}': '{value}'.", source, propName ?? name);

            /// <summary>
            /// The string type's value doesn't match given format.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error FormatInvalid(SourceInfo? source, string value, object type, string propName)
                => new Error(ErrorLevel.Warning, "format-invalid", $"String '{value}' is not a valid '{type}'.", source, propName);

            /// <summary>
            /// Array length not within min and max.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error ArrayLengthInvalid(SourceInfo? source, string propName, string criteria)
                => new Error(ErrorLevel.Warning, "array-length-invalid", $"Array '{propName}' length should be {criteria}.", source, propName);

            /// <summary>
            /// Array conditional check not within min value
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error ArrayMinCheckInvalid(SourceInfo? source, string propertyPath, int value)
                => new Error(ErrorLevel.Warning, "array-min-check-invalid", $"The array must have least {value} matched item(s).", source, propertyPath);

            /// <summary>
            /// Array conditional check not within max value
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error ArrayMaxCheckInvalid(SourceInfo? source, string propertyPath, int value)
                => new Error(ErrorLevel.Warning, "array-max-check-invalid", $"The array must not have more than {value} matched item(s).", source, propertyPath);

            /// <summary>
            /// Array items not unique.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error ArrayNotUnique(SourceInfo? source, string propName)
                => new Error(ErrorLevel.Warning, "array-not-unique", $"Array '{propName}' items should be unique.", source, propName);

            /// <summary>
            /// Array items not unique.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error ArrayContainsFailed(SourceInfo? source, string propName)
                => new Error(ErrorLevel.Warning, "array-contains-failed", $"Array '{propName}' should contain at least one item that matches JSON schema.", source, propName);

            /// <summary>
            /// Error when JSON boolean schema failed.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error BooleanSchemaFailed(SourceInfo? source, string propName)
                => new Error(ErrorLevel.Warning, "boolean-schema-failed", $"Boolean schema validation failed for '{propName}'.", source, propName);

            /// <summary>
            /// Object property count not within min and max.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error PropertyCountInvalid(SourceInfo? source, string propName, string criteria)
                => new Error(ErrorLevel.Warning, "property-count-invalid", $"Object '{propName}' property count should be {criteria}.", source, propName);

            /// <summary>
            /// String length not within min and max.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error StringLengthInvalid(SourceInfo? source, string propName, string type, int length, string criteria)
                => new Error(ErrorLevel.Warning, "string-length-invalid", $"String '{propName}' is too {type}: {length} characters. Length should be {criteria}.", source, propName);

            /// <summary>
            /// Number not within min and max.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error NumberInvalid(SourceInfo? source, double value, string criteria, string propName)
                => new Error(ErrorLevel.Warning, "number-invalid", $"Number '{value}' should be {criteria}.", source, propName);

            /// <summary>
            /// Data does not match exactly one subschema
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error OneOfFailed(SourceInfo? source, string propName, object value)
                => new Error(ErrorLevel.Warning, "one-of-failed", $"Invalid value for '{propName}': '{value}'.", source, propName);

            /// <summary>
            /// Data matches subschema
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error NotFailed(SourceInfo? source, string propName, object value)
                => new Error(ErrorLevel.Warning, "not-failed", $"Invalid value for '{propName}': '{value}'.", source, propName);

            /// <summary>
            /// A required attribute is missing.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error MissingAttribute(SourceInfo? source, string name)
                => new Error(ErrorLevel.Warning, "missing-attribute", $"Missing required attribute: '{name}'.", source, name);

            /// <summary>
            /// An attribute lacks the required dependency.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error MissingPairedAttribute(SourceInfo? source, string name, string otherKey)
                => new Error(ErrorLevel.Warning, "missing-paired-attribute", $"Missing attribute: '{otherKey}'. If you specify '{name}', you must also specify '{otherKey}'.", source, name);

            /// <summary>
            /// Attributes do not meet the requirements of either logic.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error MissingEitherAttribute(SourceInfo? source, IEnumerable<object> attributes, string propName)
                => new Error(ErrorLevel.Warning, "missing-either-attribute", $"One of the following attributes is required: {StringUtility.Join(attributes)}.", source, propName);

            /// <summary>
            /// Attributes do not meet the requirements of precludes logic.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error PrecludedAttributes(SourceInfo? source, IEnumerable<object> attributes, string propName)
                => new Error(ErrorLevel.Warning, "precluded-attributes", $"Only one of the following attributes can exist: {StringUtility.Join(attributes)}.", source, propName);

            /// <summary>
            /// An attribute doesn't conform to date format.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error DateFormatInvalid(SourceInfo? source, string name, string value)
                => new Error(ErrorLevel.Warning, "date-format-invalid", $"Invalid date format for '{name}': '{value}'.", source, name);

            /// <summary>
            /// Date out of range.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error DateOutOfRange(SourceInfo? source, string name, string value)
                => new Error(ErrorLevel.Warning, "date-out-of-range", $"Value out of range for '{name}': '{value}'.", source, name);

            /// <summary>
            /// An attribute is deprecated.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error AttributeDeprecated(SourceInfo? source, string name, string replacedBy)
                => new Error(ErrorLevel.Warning, "attribute-deprecated", $"Deprecated attribute: '{name}'{(string.IsNullOrEmpty(replacedBy) ? "." : $", use '{replacedBy}' instead.")}", source, name);

            /// <summary>
            /// The value of paired attribute is invalid.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error InvalidPairedAttribute(SourceInfo? source, string name, object value, string dependentFieldName, object? dependentFieldValue, string propName)
                => new Error(ErrorLevel.Warning, "invalid-paired-attribute", $"Invalid value for '{name}': '{value}' is not valid with '{dependentFieldName}' value '{dependentFieldValue}'.", source, propName);

            /// <summary>
            /// The value is not a valid Microsoft alias
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error MsAliasInvalid(SourceInfo<string> alias, string name)
                => new Error(ErrorLevel.Warning, "ms-alias-invalid", $"Invalid value for '{name}', '{alias}' is not a valid Microsoft alias.", alias, propertyPath: name);

            /// <summary>
            /// The attribute value is duplicated within docset
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error DuplicateAttribute(SourceInfo? source, string name, object value, IEnumerable<SourceInfo> duplicatedSources)
                => new Error(
                    ErrorLevel.Suggestion,
                    "duplicate-attribute",
                    $"Attribute '{name}' with value '{value}' is duplicated in {StringUtility.Join(duplicatedSources)}.",
                    source,
                    name);

            public static Error MinReferenceCountInvalid(SourceInfo<string>? source, int? minReferenceCount, IEnumerable<SourceInfo?> conflicts, string? propertyPath)
                => new Error(ErrorLevel.Warning, "min-reference-count-invalid", $"UID '{source}' reference count should be least {minReferenceCount}, but now is {conflicts.Count()} ({StringUtility.Join(conflicts)}).", source, propertyPath);

            public static Error MaxReferenceCountInvalid(SourceInfo<string>? source, int? maxReferenceCount, IEnumerable<SourceInfo?> conflicts, string? propertyPath)
                => new Error(ErrorLevel.Warning, "max-reference-count-invalid", $"UID '{source}' reference count should not be more than {maxReferenceCount}, but now is {conflicts.Count()} ({StringUtility.Join(conflicts)}).", source, propertyPath);
        }

        public static class Metadata
        {
            /// <summary>
            /// Failed to get a user from neither user cache nor github api by login.
            /// Examples:
            ///   - defined a non-existent author
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error AuthorNotFound(SourceInfo<string> login)
                => new Error(ErrorLevel.Warning, "author-not-found", $"Invalid value for author: '{login}' is not a valid GitHub ID.", login);

            /// <summary>
            /// - Used docfx output model property which are not defined in input model.
            /// - Define href property at the same level with uid, href value will be overwritten.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error AttributeReserved(SourceInfo? source, string name)
                => new Error(ErrorLevel.Warning, "attribute-reserved", $"Attribute {name} is reserved for use by Docs.", source, name);
        }

        public static class Content
        {
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
                => new Error(ErrorLevel.Warning, "merge-conflict", $"File contains merge conflict markers.", source);

            /// <summary>
            /// Defined reference with by #bookmark fragment within articles, which doesn't exist.
            /// </summary>
            /// Behavior: ✔️ Message: ❌
            public static Error BookmarkNotFound(SourceInfo? source, FilePath reference, string bookmark, IEnumerable<string> candidateBookmarks)
                => new Error(ErrorLevel.Warning, "bookmark-not-found", $"Cannot find bookmark '#{bookmark}' in '{reference}'{(StringUtility.FindBestMatch(bookmark, candidateBookmarks, out var matchedBookmark) ? $", did you mean '#{matchedBookmark}'?" : ".")}", source);

            /// <summary>
            /// Custom 404 page is not supported
            /// Example:
            ///   - user want their 404.md to be built and shown as their 404 page of the website.
            /// </summary>
            public static Error Custom404Page(FilePath file)
                => new Error(ErrorLevel.Warning, "custom-404-page", $"Custom 404 page will be deprecated in future. Please remove the 404.md file to resolve this warning.", new SourceInfo(file));

            /// <summary>
            /// Html Tag value must be in allowed list
            /// </summary>
            public static Error DisallowedHtml(SourceInfo? source, string tag)
                => new Error(ErrorLevel.Info, "disallowed-html", $"HTML tag '{tag}' isn't allowed. Disallowed HTML poses a security risk and must be replaced with approved Docs Markdown syntax.", source, propertyPath: tag);

            /// <summary>
            /// Html Attribute value must be in allowed list
            /// </summary>
            public static Error DisallowedHtml(SourceInfo? source, string tag, string attribute)
                => new Error(ErrorLevel.Info, "disallowed-html", $"HTML attribute '{attribute}' on tag '{tag}' isn't allowed. Disallowed HTML poses a security risk and must be replaced with approved Docs Markdown syntax.", source, propertyPath: $"{tag}_{attribute}");
        }

        public static class DependencyRepository
        {
            /// <summary>
            /// Failed to restore dependent repository
            /// Examples:
            ///   - System service account is a member of org but is not SSO enabled.
            ///   - System service account does not have sufficient permission to restore template repo.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error RestoreDependentRepositoryFailed(string url, string branch)
                => new Error(ErrorLevel.Error, "restore-dependent-repository-failed", $"Failed to restore dependent repository `{url}#{branch}`. This could be caused by an incorrect repository URL, please verify the URL on the Docs Portal (https://ops.microsoft.com). If it is not the case, please open a ticket in https://SiteHelp and include URL of the build report.");

            /// <summary>
            /// Repository owner did not re-authorize his/her GitHub account to Docs Build with SSO.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error RepositoryOwnerSSOIssue(string? repoUrl, string? repoOwner, string dependentRepoUrl)
                => new Error(ErrorLevel.Error, "repository-owner-sso-issue", $"Owner of {repoUrl} repository does not have access to {dependentRepoUrl}. Please ask the repository owner '{repoOwner}' to re-authorize his/her GitHub account to Docs Build (reference: https://teams.microsoft.com/l/message/19:7ecffca1166a4a3986fed528cf0870ee@thread.skype/1590030602688?tenantId=72f988bf-86f1-41af-91ab-2d7cd011db47&groupId=de9ddba4-2574-4830-87ed-41668c07a1ca&parentMessageId=1590030602688&teamName=Docs%20Support&channelName=General&createdTime=1590030602688).");

            /// <summary>
            /// Service accounts do not have 'Write' permissions on CRR.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error ServiceAccountPermissionInsufficient(string? repoOrg, string? repoOwner, string dependentRepoUrl)
                => new Error(ErrorLevel.Error, "service-account-permission-insufficient", $"Docs Build service account cannot access repository '{dependentRepoUrl}'. Please ask repository owner '{repoOwner}' to grant 'write' permission to all service accounts under '{repoOrg}' organization to '{dependentRepoUrl}'. Service accounts list can be found here: https://review.docs.microsoft.com/en-us/engineering/projects/ops/engdocs/how-to-grant-service-account-permission-in-your-repository?branch=master#{repoOrg?.ToLowerInvariant()}. For any support, please open a ticket in https://SiteHelp.");

            /// <summary>
            /// Repository owner does not have 'Read' permission on CRR.
            /// </summary>
            /// Behavior: ✔️ Message: ✔️
            public static Error RepositoryOwnerPermissionInsufficient(string? repoOwner, string? dependentRepoOrg, string? dependentRepoName, string dependentRepoUrl)
                => new Error(ErrorLevel.Error, "repository-owner-permission-insufficient", $"Docs Build cannot access CRR repo {dependentRepoUrl} using the access token from user {repoOwner} because {repoOwner} does not have Read access to the CRR repo. Please ask {repoOwner} to contact the admins of the CRR repo {dependentRepoUrl} to get Read permission. Don't know who to contact? This page contains admin information of the CRR repo if it is owned by Microsoft: https://repos.opensource.microsoft.com/{dependentRepoOrg}/repos/{dependentRepoName}/permissions/");
        }

        public static class Template
        {
            /// <summary>
            /// Mustache is not found for current mime type.
            /// </summary>
            /// Behavior: ❌ Message: ❌
            public static Error MustacheNotFound(string templateFileName)
                => new Error(ErrorLevel.Error, "mustache-not-found", $"Mustache template is not found at '{templateFileName}'.");
        }

        public static class SourceMap
        {
            public static Error DuplicateSourceMapItem(string key, IEnumerable<PathString> originalFiles)
                => new Error(ErrorLevel.Warning, "duplicate-source-map-item", $"'{key}' is duplicated from {StringUtility.Join(originalFiles)}");
        }
    }
}
