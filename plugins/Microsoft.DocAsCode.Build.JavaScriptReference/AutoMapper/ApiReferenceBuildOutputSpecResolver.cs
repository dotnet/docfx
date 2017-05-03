// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using Microsoft.DocAsCode.DataContracts.Common;

    using AutoMapper;

    public class ApiReferenceBuildOutputSpecResolver : IValueResolver<ReferenceViewModel, ApiReferenceBuildOutput, List<ApiLanguageValuePair>>
    {
        private readonly string[] _supportedLanguages;

        public ApiReferenceBuildOutputSpecResolver(string[] supportedLanguages)
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

        public List<ApiLanguageValuePair> Resolve(ReferenceViewModel source, ApiReferenceBuildOutput destination, List<ApiLanguageValuePair> destMember, ResolutionContext context)
        {
            var specs = source.Specs;
            if (specs != null && specs.Count > 0)
            {
                return specs
                    .Where(kv => _supportedLanguages.Contains(kv.Key))
                    .Select(kv => new ApiLanguageValuePair
                    {
                        Language = kv.Key,
                        Value = GetSpecName(kv.Value)
                    }).ToList();
            }
            var xref = GetXref(source.Uid, source.Name, source.FullName);
            return _supportedLanguages
                .Select(s => new ApiLanguageValuePair
                {
                    Language = s,
                    Value = xref
                }).ToList();
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
            return GetXref(svm.Uid, svm.Name, svm.FullName);
        }

        private static string GetXref(string uid, string text = null, string alt = null)
        {
            var result = $"<xref href=\"{HttpUtility.HtmlEncode(uid)}\"";
            if (!string.IsNullOrEmpty(text))
            {
                result += $" text=\"{HttpUtility.HtmlEncode(text)}\"";
            }
            else
            {
                result += " displayProperty=\"name\"";
            }
            if (!string.IsNullOrEmpty(alt))
            {
                result += $" alt=\"{HttpUtility.HtmlEncode(alt)}\"";
            }
            else
            {
                result += " altProperty=\"fullName\"";
            }
            result += "/>";
            return result;
        }
    }
}
