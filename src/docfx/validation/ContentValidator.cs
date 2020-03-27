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

            var validationContext = new ValidationContext { DocumentType = string.IsNullOrEmpty(file.Mime) ? "conceptual" : file.Mime.Value! };
            Write(_validator.ValidateHeadings(headings, validationContext).GetAwaiter().GetResult());
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
                    _ => ErrorLevel.Off
                };
        }
    }
}
