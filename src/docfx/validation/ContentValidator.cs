// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
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

        internal void ValidateH1(Document file, string? title)
        {
            if (string.IsNullOrEmpty(title))
                return;

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
            if (TryGetDocumentType(file, out string documentType))
            {
                var validationContext = new ValidationContext { DocumentType = documentType };
                Write(_validator.ValidateHeadings(headings, validationContext).GetAwaiter().GetResult());
            }
        }

        internal void ValidateManifest(Document file)
        {
            var manifestItem = new ManifestItem()
            {
                PublishUrl = file.SiteUrl,
                SourceInfo = new SourceInfo(file.FilePath, 0, 0),
            };

            if (TryGetDocumentType(file, out string documentType))
            {
                var validationContext = new ValidationContext { DocumentType = documentType };
                Write(_validator.ValidateManifest(manifestItem, validationContext).GetAwaiter().GetResult());
            }
        }

        internal void PostValidate()
        {
            Write(_validator.PostValidate().GetAwaiter().GetResult());
        }

        internal static string GetMarkdownValidationRulesFilePath(FileResolver fileResolver, Config config)
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
        private bool TryGetDocumentType(Document document, out string documentType)
        {
            documentType = string.Empty;
            switch (document.ContentType)
            {
                case ContentType.Page:
                    if (!string.IsNullOrEmpty(document.Mime))
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
