// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AutoMapper;

    public class ApiReferenceBuildOutputConverter : ITypeConverter<string, ApiReferenceBuildOutput>
    {
        private readonly string[] _supportedLanguages;

        private readonly IReadOnlyDictionary<string, ApiReferenceBuildOutput> _references;

        public ApiReferenceBuildOutputConverter(string[] supportedLanguages, IReadOnlyDictionary<string, ApiReferenceBuildOutput> references)
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
            _references = references ?? new Dictionary<string, ApiReferenceBuildOutput>();
        }

        public ApiReferenceBuildOutput Convert(string source, ApiReferenceBuildOutput destination, ResolutionContext context)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            if (_references.TryGetValue(source, out ApiReferenceBuildOutput result))
            {
                return result;
            }

            return new ApiReferenceBuildOutput
            {
                Spec = _supportedLanguages.Select(s => new ApiLanguageValuePair
                {
                    Language = s,
                    Value = source
                }).ToList()
            };
        }
    }
}
