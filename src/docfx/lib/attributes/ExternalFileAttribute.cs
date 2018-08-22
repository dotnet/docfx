// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Microsoft.Docs.Build
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ExternalFileAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!validationContext.Items.TryGetValue("docsetPath", out var docsetPath))
            {
                throw Errors.ValidationContextMissing(nameof(docsetPath)).ToException();
            }

            if (!string.IsNullOrEmpty(value.ToString())
                && !HrefUtility.IsHttpHref(value.ToString())
                && !File.Exists(Path.Combine(docsetPath.ToString(), value.ToString())))
            {
                return new ValidationResult($"External file not found at '{value}'", null);
            }
            return null;
        }
    }
}
