// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Markdig.Syntax;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build
{
    internal class ContentValidator : ICollectionFactory
    {
        // Now Docs.Validation only support conceptual page, redirection page and toc file. Other type will be supported later.
        // Learn content: "learningpath", "module", "moduleunit"
        private static readonly string[] s_supportedPageTypes =
        {
            "conceptual", "includes", "toc", "redirection", "learningpath", "module", "moduleunit", "zonepivotgroups", "post",
        };

        private readonly Config _config;
        private readonly Validator _validator;
        private readonly ErrorBuilder _errors;
        private readonly DocumentProvider _documentProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly ZonePivotProvider _zonePivotProvider;
        private readonly PublishUrlMap _publishUrlMap;

        public ContentValidator(
            Config config,
            FileResolver fileResolver,
            ErrorBuilder errors,
            DocumentProvider documentProvider,
            MonikerProvider monikerProvider,
            ZonePivotProvider zonePivotProvider,
            PublishUrlMap publishUrlMap)
        {
            _config = config;
            _errors = errors;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;
            _zonePivotProvider = zonePivotProvider;
            _publishUrlMap = publishUrlMap;

            _validator = new Validator(
                fileResolver.ResolveFilePath(_config.MarkdownValidationRules),
                fileResolver.ResolveFilePath(_config.Allowlists),
                fileResolver.ResolveFilePath(_config.SandboxEnabledModuleList),
                this);
        }

        public void ValidateLink(FilePath file, SourceInfo<string> link, MarkdownObject origin, bool isImage, string? altText, int imageIndex)
        {
            // validate image link and altText here
            if (TryCreateValidationContext(file, out var validationContext))
            {
                var item = new LinkNode
                {
                    UrlLink = link,
                    AltText = altText,
                    IsImage = isImage,
                    IsInlineImage = origin.IsInlineImage(imageIndex),
                    SourceInfo = link.Source,
                    ParentSourceInfoList = origin.GetInclusionStack(),
                    Monikers = origin.GetZoneLevelMonikers(),
                    ZonePivots = origin.GetZonePivots(),
                    TabbedConceptualHeader = origin.GetTabId(),
                };
                Write(_validator.ValidateLink(item, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateCodeBlock(FilePath file, CodeBlockNode codeBlockItem)
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
                Write(_validator.ValidateContentNodes(nodes, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateHierarchy(List<HierarchyNode> models)
        {
            Write(_validator.ValidateHierarchy(models).GetAwaiter().GetResult());
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
                var canonicalVersion = _publishUrlMap.GetCanonicalVersion(file);
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
                var textItem = new TextNode()
                {
                    Content = content,
                    SourceInfo = new SourceInfo(file),
                };

                Write(_validator.ValidateText(textItem, validationContext).GetAwaiter().GetResult());
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

        public void ValidateTocBreadcrumbLinkExternal(FilePath file, SourceInfo<TocNode> node)
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

        public void ValidateZonePivotDefinition(FilePath file, ZonePivotGroupDefinition definition)
        {
            if (TryCreateValidationContext(file, false, out var validationContext))
            {
                Write(_validator.ValidateZonePivot(definition, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateZonePivots(FilePath file, List<SourceInfo<string>> zonePivotUsages)
        {
            // Build types other than Docs is not supported
            if (_config.UrlType != UrlType.Docs)
            {
                return;
            }

            // No need to run validation if pivot not used in this page
            if (!zonePivotUsages.Any())
            {
                return;
            }

            var zonePivotGroup = _zonePivotProvider.TryGetZonePivotGroups(file);
            if (zonePivotGroup == null)
            {
                // Unable to load definition file or group
                return;
            }

            if (TryCreateValidationContext(file, false, out var validationContext))
            {
                validationContext = validationContext with
                {
                    ZonePivotContext = (zonePivotGroup.Value.DefinitionFile, zonePivotGroup.Value.PivotGroups),
                };
                List<(string, object)> usages = zonePivotUsages.Select(u => (u.Value, (object)u.Source!)).ToList();
                Write(_validator.ValidateZonePivot(usages, validationContext).GetAwaiter().GetResult());
            }
        }

        public void PostValidate()
        {
            Write(_validator.PostValidate().GetAwaiter().GetResult());
        }

        public IProducerConsumerCollection<T> CreateCollection<T>()
        {
            return new ScopedConcurrentBag<T>();
        }

        private void Write(IEnumerable<ValidationError> validationErrors)
        {
            _errors.AddRange(validationErrors.Select(ToError));

            static Error ToError(ValidationError e)
            {
                var level = e.Severity switch
                {
                    ValidationSeverity.SUGGESTION => ErrorLevel.Suggestion,
                    ValidationSeverity.WARNING => ErrorLevel.Warning,
                    ValidationSeverity.ERROR => ErrorLevel.Error,
                    _ => ErrorLevel.Off,
                };

                var source = e.SourceInfo is SourceInfo sourceInfo
                    ? e.LineOffset > 0 || e.ColumnOffset > 0 ? sourceInfo.WithOffset(e.LineOffset + 1, e.ColumnOffset + 1) : sourceInfo
                    : null;

                return new Error(level, e.Code, $"{e.Message}", source);
            }
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
                documentType = "include";
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
