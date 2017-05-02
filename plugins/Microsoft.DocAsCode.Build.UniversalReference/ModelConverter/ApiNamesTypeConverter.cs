// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System;
    using System.Collections.Generic;

    using AutoMapper;

    public class ApiNamesTypeConverter : ITypeConverter<string, ApiNames>
    {
        private readonly string[] _supportedLanguages;

        private readonly IReadOnlyDictionary<string, ApiNames> _references;

        public ApiNamesTypeConverter(string[] supportedLanguages, IReadOnlyDictionary<string, ApiNames> references)
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

        public ApiNames Convert(string source, ApiNames destination, ResolutionContext context)
        {
            return ModelConverter.ResolveApiNames(source, _supportedLanguages, _references);
        }
    }
}
