// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using Microsoft.DocAsCode.DataContracts.Common;

    using AutoMapper;

    public class ApiNamesSpecResolver : IValueResolver<ReferenceViewModel, ApiNames, List<ApiLanguageValuePair<string>>>
    {
        private readonly string[] _supportedLanguages;

        public ApiNamesSpecResolver(string[] supportedLanguages)
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
        }

        public List<ApiLanguageValuePair<string>> Resolve(ReferenceViewModel source, ApiNames destination, List<ApiLanguageValuePair<string>> destMember, ResolutionContext context)
        {
            var result = new List<ApiLanguageValuePair<string>>();
            var specs = source.Specs;
            foreach (var language in _supportedLanguages)
            {
               if (specs?.ContainsKey(language) == true)
                {
                    result.Add(new ApiLanguageValuePair<string>
                    {
                        Language = language,
                        Value = GetSpecName(specs[language])
                    });
                }
                else
                {
                    result.Add(new ApiLanguageValuePair<string>
                    {
                        Language = language,
                        Value = ModelConverter.GetXref(source.Uid, source.Name, source.FullName)
                    });
                }
            }
            return result;
        }

        private static string GetSpecName(List<SpecViewModel> spec)
        {
            return spec == null ? null : string.Concat(spec.Select(GetCompositeName));
        }

        private static string GetCompositeName(SpecViewModel svm)
        {
            // If href does not exists, return full name
            if (string.IsNullOrEmpty(svm.Uid)) { return HttpUtility.HtmlEncode(svm.FullName); }

            // If href exists, return name with href
            return ModelConverter.GetXref(svm.Uid, svm.Name, svm.FullName);
        }
    }
}
