// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AutoMapper;

    public class ApiLanguageValuePairTypeConverter : ITypeConverter<string, List<ApiLanguageValuePair>>
    {
        private readonly string[] _supportedLanguages;

        public ApiLanguageValuePairTypeConverter(string[] supportedLanguages)
        {
            if (supportedLanguages == null)
            {
                throw new ArgumentException(nameof(supportedLanguages));
            }
            if (supportedLanguages.Length == 0)
            {
                throw new ArgumentException(nameof(supportedLanguages));
            }
            _supportedLanguages = supportedLanguages;
        }

        public List<ApiLanguageValuePair> Convert(string source, List<ApiLanguageValuePair> destination, ResolutionContext context)
        {
            return _supportedLanguages.Select(l => new ApiLanguageValuePair
            {
                Language = l,
                Value = source
            }).ToList();
        }
    }
}
