// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public ContentValidator(Config config, FileResolver fileResolver, ErrorLog log)
        {
            _validator = new Validator(GetMarkdownValidationRulesFilePath(fileResolver, config));
            _errorLog = log;
        }

        public void ValidateH1(Document file, string? title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return;
            }

            if (TryGetValidationDocumentType(file.ContentType, file.Mime.Value, out var documentType))
            {
                var headings = new List<Heading>
                {
                    new Heading
                    {
                        Level = 1,
                        Content = title,

                        // todo: get title precise line info
                        SourceInfo = new SourceInfo(file.FilePath, 0, 0),
                    },
                };
                var validationContext = new ValidationContext { DocumentType = documentType };
                Write(_validator.ValidateHeadings(headings, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateHeadings(Document file, List<Heading> headings)
        {
            if (TryGetValidationDocumentType(file.ContentType, file.Mime.Value, out var documentType))
            {
                var validationContext = new ValidationContext { DocumentType = documentType };
                Write(_validator.ValidateHeadings(headings, validationContext).GetAwaiter().GetResult());
            }
        }

        public void ValidateManifest(FilePath filePath, PublishItem publishItem)
        {
            if (TryGetValidationDocumentType(publishItem.ContentType, publishItem.Mime, out var documentType))
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

        public void PostValidate()
        {
            Write(_validator.PostValidate().GetAwaiter().GetResult());
        }

        public static string GetMarkdownValidationRulesFilePath(FileResolver fileResolver, Config config)
        {
            string filePath = config.MarkdownValidationRules;
            if (!string.IsNullOrEmpty(filePath))
            {
                using var stream = fileResolver.ReadStream(config.MarkdownValidationRules);

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
        private bool TryGetValidationDocumentType(ContentType contentType, string? mime, out string documentType)
        {
            documentType = string.Empty;
            switch (contentType)
            {
                case ContentType.Page:
                    if (mime != "Conceptual")
                    {
                        return false;
                    }
                    documentType = "conceptual";
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
