// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.BuildOutputs
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdownLite;

    public static class ApiBuildOutputUtility
    {
        public static ApiReferenceBuildOutput GetReferenceViewModel(string key, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (string.IsNullOrEmpty(key)) return null;

            ApiReferenceBuildOutput ervm;
            if (!references.TryGetValue(key, out ervm))
            {
                var spec = GetXref(key);
                ervm = new ApiReferenceBuildOutput
                {
                    Spec = ApiReferenceBuildOutput.GetSpecNames(GetXref(key), supportedLanguages),
                };
            }
            else
            {
                ervm.Expand(references, supportedLanguages);
            }

            return ervm;
        }

        public static ApiReferenceBuildOutput GetReferenceViewModel(string key, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages, int index)
        {
            var ervm = GetReferenceViewModel(key, references, supportedLanguages);
            ervm.Index = index;
            return ervm;
        }

        public static List<ApiLanguageValuePair> TransformToLanguagePairList(string defaultValue, SortedList<string, string> values, string[] supportedLanguages)
        {
            if (string.IsNullOrEmpty(defaultValue) || supportedLanguages == null || supportedLanguages.Length == 0) return null;

            var result = new List<ApiLanguageValuePair>();
            foreach (var language in supportedLanguages)
            {
                result.Add(new ApiLanguageValuePair
                {
                    Language = language,
                    Value = values.ContainsKey(language) ? values[language] : defaultValue,
                });
            }

            return result;
        }

        public static string GetXref(string uid, string fullName = null, string name = null)
        {
            var xref = $"<xref href=\"{StringHelper.HtmlEncode(uid)}\"";
            if (!string.IsNullOrEmpty(fullName)) xref += $" fullName=\"{StringHelper.HtmlEncode(fullName)}\"";
            if (!string.IsNullOrEmpty(name)) xref += $" name=\"{StringHelper.HtmlEncode(name)}\"";
            xref += "/>";
            return xref;
        }
    }
}
