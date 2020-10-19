// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build
{
    internal class ContentValidator
    {
        // Now Docs.Validation only support conceptual page, redirection page and toc file. Other type will be supported later.
        // Learn content: "learningpath", "module", "moduleunit"
        private static readonly string[] s_supportedPageTypes = { "conceptual", "includes", "toc", "redirection", "learningpath", "module", "moduleunit" };

        private readonly Validator _validator;
        private readonly ErrorBuilder _errors;
        private readonly DocumentProvider _documentProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly ZonePivotProvider _zonePivotProvider;
        private readonly Lazy<PublishUrlMap> _publishUrlMap;
        private readonly ConcurrentHashSet<(FilePath, SourceInfo<string>)> _links = new ConcurrentHashSet<(FilePath, SourceInfo<string>)>();

        public ContentValidator(
            Config config,
            FileResolver fileResolver,
            ErrorBuilder errors,
            DocumentProvider documentProvider,
            MonikerProvider monikerProvider,
            ZonePivotProvider zonePivotProvider,
            Lazy<PublishUrlMap> publishUrlMap)
        {
            _errors = errors;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;
            _zonePivotProvider = zonePivotProvider;
            _publishUrlMap = publishUrlMap;

            _validator = new Validator(
                fileResolver.ResolveFilePath(config.MarkdownValidationRules),
                fileResolver.ResolveFilePath(config.Allowlists),
                fileResolver.ResolveFilePath(config.Disallowlists));
        }

        public void ValidateImageLink(FilePath file, SourceInfo<string> link, MarkdownObject origin, string? altText, int imageIndex)
        {
            // validate image link and altText here
            if (_links.TryAdd((file, link)) && TryCreateValidationContext(file, out var validationContext))
            {
                Write(_validator.ValidateLink(
                    new Link
                    {
                        UrlLink = link,
                        AltText = altText,
                        IsImage = true,
                        IsInlineImage = origin.IsInlineImage(imageIndex),
                        SourceInfo = link.Source,
                        ParentSourceInfoList = origin.GetInclusionStack(),
                        Monikers = origin.GetZoneLevelMonikers(),
                    }, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateCodeBlock(FilePath file, CodeBlockItem codeBlockItem)
        {
            if (TryCreateValidationContext(file, out var validationContext))
            {
                Write(_validator.ValidateCodeBlock(codeBlockItem, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateHeadings(FilePath file, List<ContentNode> nodes)
        {
            if (TryCreateValidationContext(file, out var validationContext))
            {
                Write(_validator.ValidateHeadings(nodes, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateTitle(FilePath file, SourceInfo<string?> title, string? titleSuffix)
        {
            if (string.IsNullOrWhiteSpace(title.Value))
            {
                return;
            }

            if (TryGetValidationDocumentType(file, out var documentType))
            {
                var monikers = _monikerProvider.GetFileLevelMonikers(_errors, file);
                var canonicalVersion = _publishUrlMap.Value.GetCanonicalVersion(file);
                var isCanonicalVersion = monikers.IsCanonicalVersion(canonicalVersion);
                var titleItem = new TitleItem
                {
                    IsCanonicalVersion = isCanonicalVersion,
                    Title = GetOgTitle(title.Value, titleSuffix),
                    SourceInfo = title.Source,
                };
                var validationContext = new ValidationContext
                {
                    DocumentType = documentType,
                    FileSourceInfo = new SourceInfo(file),
                    Monikers = monikers,
                };
                Write(_validator.ValidateTitle(titleItem, validationContext).GetAwaiter().GetResult());
            }

            static string GetOgTitle(string title, string? titleSuffix)
            {
                // below code logic is copied from docs-ui, but not exactly same
                if (string.IsNullOrWhiteSpace(titleSuffix))
                {
                    return title;
                }

                var pipeIndex = title.IndexOf('|');
                if (pipeIndex > 5)
                {
                    return $"{title.Substring(0, pipeIndex)} - {titleSuffix}";
                }
                return $"{title} - {titleSuffix}";
            }
        }

        public void ValidateSensitiveLanguage(FilePath file, string content)
        {
            if (TryCreateValidationContext(file, false, out var validationContext))
            {
                using var reader = new StringReader(content);
                var lineCount = 1;
                string? line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    var item = new TextItem()
                    {
                        Content = line,
                        SourceInfo = new SourceInfo(file, lineCount++, 0),
                    };
                    Write(_validator.ValidateText(item, validationContext).GetAwaiter().GetResult());
                }
            }
        }

        public void ValidateManifest(FilePath file)
        {
            if (TryCreateValidationContext(file, false, out var validationContext))
            {
                var manifestItem = new ManifestItem()
                {
                    PublishUrl = _documentProvider.GetSiteUrl(file),
                    SourceInfo = new SourceInfo(file),
                };

                Write(_validator.ValidateManifest(manifestItem, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateTocDeprecated(FilePath file)
        {
            if (TryCreateValidationContext(file, false, out var validationContext))
            {
                var tocItem = new DeprecatedTocItem()
                {
                    FilePath = file.Path.Value,
                    SourceInfo = new SourceInfo(file),
                };
                Write(_validator.ValidateToc(tocItem, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateTocMissing(FilePath file, bool hasReferencedTocs)
        {
            if (TryCreateValidationContext(file, false, out var validationContext))
            {
                var tocItem = new MissingTocItem()
                {
                    FilePath = file.Path.Value,
                    HasReferencedTocs = hasReferencedTocs,
                    SourceInfo = new SourceInfo(file),
                };
                Write(_validator.ValidateToc(tocItem, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateTocBreadcrumbLinkExternal(FilePath file, SourceInfo<TableOfContentsNode> node)
        {
            if (!string.IsNullOrEmpty(node.Value?.Href)
                && TryCreateValidationContext(file, false, out var validationContext))
            {
                var tocItem = new ExternalBreadcrumbTocItem()
                {
                    FilePath = node.Value.Href!,
                    IsHrefExternal = UrlUtility.GetLinkType(node.Value.Href) == LinkType.External,
                    SourceInfo = node.Source,
                };
                Write(_validator.ValidateToc(tocItem, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateTocEntryDuplicated(FilePath file, List<FilePath> referencedFiles)
        {
            if (TryCreateValidationContext(file, false, out var validationContext))
            {
                var filePaths = referencedFiles
                    .Select(item => item.Path.Value)
                    .ToList();

                var tocItem = new DuplicatedTocItem()
                {
                    FilePaths = filePaths,
                    SourceInfo = new SourceInfo(file),
                };
                Write(_validator.ValidateToc(tocItem, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateZonePivots(FilePath file, List<(TripleColonBlock, string)> zonePivotUsages)
        {
            var (definitionFile, group) = _zonePivotProvider.GetZonePivotGroup(file);
            var usage = group == null ? new Dictionary<string, bool>() : group.Pivots.Select(p => p.Id).Distinct().ToDictionary(p => p, _ => false);
            if ((definitionFile == null || group == null) && zonePivotUsages.Any())
            {
                // TODO: throw error because we are unable to load definition file or group, yet got zone pivot usages
                return;
            }

            foreach (var (tripleColon, pivotId) in zonePivotUsages)
            {
                if (usage.ContainsKey(pivotId))
                {
                    usage[pivotId] = true;
                }
                else
                {
                    _errors.Add(Errors.ZonePivot.ZonePivotIdNotFound(tripleColon.GetSourceInfo(), pivotId, group!.Id, definitionFile));
                }
            }

            _errors.AddRange(usage.Where(p => !p.Value).Select(p => Errors.ZonePivot.ZonePivotIdUnused(new SourceInfo(file), p.Key, group!.Id, definitionFile)));
        }

        public void PostValidate()
        {
            Write(_validator.PostValidate().GetAwaiter().GetResult());
        }

        private void Write(IEnumerable<ValidationError> validationErrors)
        {
            _errors.AddRange(validationErrors.Select(e => new Error(GetLevel(e.Severity), e.Code, $"{e.Message}", (SourceInfo?)e.SourceInfo)));

            static ErrorLevel GetLevel(ValidationSeverity severity) =>
                severity switch
                {
                    ValidationSeverity.SUGGESTION => ErrorLevel.Suggestion,
                    ValidationSeverity.WARNING => ErrorLevel.Warning,
                    ValidationSeverity.ERROR => ErrorLevel.Error,
                    _ => ErrorLevel.Off,
                };
        }

        private bool TryGetValidationDocumentType(FilePath file, [NotNullWhen(true)] out string? documentType)
        {
            return TryGetValidationDocumentType(file, false, out documentType);
        }

        private bool TryGetValidationDocumentType(FilePath file, bool isInclude, [NotNullWhen(true)] out string? documentType)
        {
            documentType = _documentProvider.GetPageType(file);
            if (isInclude && documentType == "conceptual")
            {
                documentType = "includes";
                return true;
            }

            return documentType != null && s_supportedPageTypes.Contains(documentType);
        }

        private bool TryCreateValidationContext(FilePath file, [NotNullWhen(true)] out ValidationContext? context)
        {
            return TryCreateValidationContext(file, true, out context);
        }

        private bool TryCreateValidationContext(FilePath file, bool needMonikers, [NotNullWhen(true)] out ValidationContext? context)
        {
            if (TryGetValidationDocumentType(file, out var documentType))
            {
                context = new ValidationContext
                {
                    DocumentType = documentType,
                    FileSourceInfo = new SourceInfo(file),
                    Monikers = GetMonikers(file, needMonikers),
                };
                return true;
            }
            else
            {
                context = null;
                return false;
            }

            IReadOnlyCollection<string>? GetMonikers(FilePath file, bool needMonikers)
            {
                if (needMonikers)
                {
                    return _monikerProvider.GetFileLevelMonikers(_errors, file);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
