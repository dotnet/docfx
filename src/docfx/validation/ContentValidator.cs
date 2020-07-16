// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build
{
    internal class ContentValidator
    {
        private Validator _validator;
        private ErrorLog _errorLog;
        private MonikerProvider _monikerProvider;
        private Lazy<PublishUrlMap> _publishUrlMap;

        public ContentValidator(Config config, FileResolver fileResolver, ErrorLog log, MonikerProvider monikerProvider, Lazy<PublishUrlMap> publishUrlMap)
        {
            _validator = new Validator(GetValidationPhysicalFilePath(fileResolver, config.MarkdownValidationRules));
            _errorLog = log;
            _monikerProvider = monikerProvider;
            _publishUrlMap = publishUrlMap;
        }

        public void ValidateHeadings(Document file, List<ContentNode> nodes, bool isIncluded)
        {
            if (TryGetValidationDocumentType(file.ContentType, file.Mime.Value, isIncluded, out var documentType))
            {
                var validationContext = new ValidationContext { DocumentType = documentType };
                Write(_validator.ValidateHeadings(nodes, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateTitle(Document file, SourceInfo<string?> title, string? titleSuffix)
        {
            if (string.IsNullOrWhiteSpace(title.Value))
            {
                return;
            }

            if (TryGetValidationDocumentType(file.ContentType, file.Mime.Value, false, out var documentType))
            {
                var (_, monikers) = _monikerProvider.GetFileLevelMonikers(file.FilePath);
                var canonicalVersion = _publishUrlMap.Value.GetCanonicalVersion(file.SiteUrl);
                var isCanonicalVersion = MonikerList.IsCanonicalVersion(canonicalVersion, monikers);
                var titleItem = new TitleItem
                {
                    IsCanonicalVersion = isCanonicalVersion,
                    Title = GetOgTitle(title.Value, titleSuffix),
                    SourceInfo = title.Source,
                };
                var validationContext = new ValidationContext { DocumentType = documentType };
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

        public void ValidateSensitiveLanguage(string content, Document document)
        {
            if (TryGetValidationDocumentType(document.ContentType, document.Mime.Value, false, out var documentType))
            {
                var validationContext = new ValidationContext { DocumentType = documentType };

                using (StringReader reader = new StringReader(content))
                {
                    var lineCount = 1;
                    string? line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var item = new TextItem()
                        {
                            Content = line,
                            SourceInfo = new SourceInfo(document.FilePath, lineCount++, 0),
                        };
                        Write(_validator.ValidateText(item, validationContext).GetAwaiter().GetResult());
                    }
                }
            }
        }

        public void ValidateManifest(FilePath filePath, PublishItem publishItem)
        {
            if (TryGetValidationDocumentType(publishItem.ContentType, publishItem.Mime, false, out var documentType))
            {
                var manifestItem = new ManifestItem()
                {
                    PublishUrl = publishItem.Url,
                    SourceInfo = new SourceInfo(filePath, 0, 0),
                };

                var validationContext = new ValidationContext { DocumentType = documentType };
                Write(_validator.ValidateManifest(manifestItem, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateTocDeprecated(FilePath filePath)
        {
            if (TryGetValidationDocumentType(ContentType.TableOfContents, string.Empty, false, out var documentType))
            {
                var validationContext = new ValidationContext { DocumentType = documentType };
                var tocItem = new DeprecatedTocItem()
                {
                    FilePath = filePath.Path.Value,
                    SourceInfo = new SourceInfo(filePath, 0, 0),
                };
                Write(_validator.ValidateToc(tocItem, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateTocMissing(Document document, bool hasReferencedTocs)
        {
            if (TryGetValidationDocumentType(document.ContentType, document.Mime.Value, false, out var documentType))
            {
                var validationContext = new ValidationContext { DocumentType = documentType };
                var tocItem = new MissingTocItem()
                {
                    FilePath = document.FilePath.Path.Value,
                    HasReferencedTocs = hasReferencedTocs,
                    SourceInfo = new SourceInfo(document.FilePath, 0, 0),
                };
                Write(_validator.ValidateToc(tocItem, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateTocBreadcrumbLinkExternal(SourceInfo<TableOfContentsNode> node)
        {
            if (!string.IsNullOrEmpty(node.Value?.Href)
                && TryGetValidationDocumentType(ContentType.TableOfContents, string.Empty, false, out var documentType))
            {
                var validationContext = new ValidationContext { DocumentType = documentType };
                var tocItem = new ExternalBreadcrumbTocItem()
                {
                    FilePath = node.Value.Href!,
                    IsHrefExternal = UrlUtility.GetLinkType(node.Value.Href) == LinkType.External,
                    SourceInfo = node.Source,
                };
                Write(_validator.ValidateToc(tocItem, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateTocEntryDuplicated(Document file, List<Document> referencedFiles)
        {
            if (TryGetValidationDocumentType(ContentType.TableOfContents, string.Empty, false, out var documentType))
            {
                var filePaths = referencedFiles
                    .Where(item => item != null)
                    .Select(item => item.FilePath.Path.Value)
                    .ToList();

                var validationContext = new ValidationContext { DocumentType = documentType };
                var tocItem = new DuplicatedTocItem()
                {
                    FilePaths = filePaths,
                    SourceInfo = new SourceInfo(file.FilePath, 0, 0),
                };
                Write(_validator.ValidateToc(tocItem, validationContext).GetAwaiter().GetResult());
            }
        }

        public void PostValidate()
        {
            Write(_validator.PostValidate().GetAwaiter().GetResult());
        }

        public static string GetValidationPhysicalFilePath(FileResolver fileResolver, SourceInfo<string> configFilePath)
        {
            string filePath = configFilePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                using var stream = fileResolver.ReadStream(configFilePath);

                // TODO: validation rules currently only supports physical file.
                filePath = ((FileStream)stream).Name;
            }

            return filePath;
        }

        private bool Write(IEnumerable<ValidationError> validationErrors)
        {
            return _errorLog.Write(validationErrors.Select(e => new Error(GetLevel(e.Severity), e.Code, e.Message, (SourceInfo?)e.SourceInfo)));

            static ErrorLevel GetLevel(ValidationSeverity severity) =>
                severity switch
                {
                    ValidationSeverity.SUGGESTION => ErrorLevel.Suggestion,
                    ValidationSeverity.WARNING => ErrorLevel.Warning,
                    ValidationSeverity.ERROR => ErrorLevel.Error,
                    _ => ErrorLevel.Off,
                };
        }

        // Now Docs.Validation only support conceptual page, redirection page and toc file. Other type will be supported later.
        private bool TryGetValidationDocumentType(ContentType contentType, string? mime, bool isIncluded, out string documentType)
        {
            documentType = string.Empty;
            switch (contentType)
            {
                case ContentType.Page:
                    if (mime != "Conceptual")
                    {
                        return false;
                    }
                    documentType = isIncluded ? "includes" : "conceptual";
                    return true;
                case ContentType.Redirection:
                    documentType = "redirection";
                    return true;
                case ContentType.TableOfContents:
                    documentType = "toc";
                    return true;
                case ContentType.Resource:
                case ContentType.Unknown:
                default:
                    return false;
            }
        }
    }
}
