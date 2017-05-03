// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using AutoMapper;

    public class ApiListInDevLangsOfApiNamesTypeConverter : ITypeConverter<string, List<ApiLanguageValuePair<ApiNames>>>
    {
        private readonly string[] _supportedLanguages;

        private readonly IReadOnlyDictionary<string, ApiNames> _references;

        public ApiListInDevLangsOfApiNamesTypeConverter(string[] supportedLanguages, IReadOnlyDictionary<string, ApiNames> references)
        {
            if (supportedLanguages == null)
            {
                throw new ArgumentNullException(nameof(supportedLanguages));
            }
            if (supportedLanguages.Length == 0)
            {
                throw new ArgumentException($"{nameof(supportedLanguages)} cannot be empty");
            }
            _supportedLanguages = supportedLanguages;
            _references = references ?? new Dictionary<string, ApiNames>();
        }

        public List<ApiLanguageValuePair<ApiNames>> Convert(string source, List<ApiLanguageValuePair<ApiNames>> destination, ResolutionContext context)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            return _supportedLanguages.Select(l =>
            {
                return new ApiLanguageValuePair<ApiNames>
                {
                    Language = l,
                    Value = ModelConverter.ResolveApiNames(source, _supportedLanguages, _references)
                };
            }).ToList();
        }

    }
}
