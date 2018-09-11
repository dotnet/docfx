// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Docs.Build
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class LocaleAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            try
            {
                var culture = new CultureInfo(value.ToString());
                return null;
            }
            catch (CultureNotFoundException)
            {
                return new ValidationResult($"Locale '{value.ToString()}' is not supported", null);
            }
        }
    }
}
