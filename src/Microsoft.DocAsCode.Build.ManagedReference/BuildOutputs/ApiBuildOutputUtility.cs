// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System.Collections.Generic;
    using System.Web;

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

        public static ApiNames GetApiNames(string key, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (string.IsNullOrEmpty(key)) return null;

            var result = ApiNames.FromUid(key);
            ApiReferenceBuildOutput ervm;
            if (references.TryGetValue(key, out ervm))
            {
                result.Definition = ervm.Definition;
                result.Name = ervm.Name;
                result.NameWithTpye = ervm.NameWithType;
                result.FullName = ervm.FullName;
                result.Spec = ervm.Spec;
            }

            return result;
        }

        public static List<ApiLanguageValuePair> GetSpec(string key, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (string.IsNullOrEmpty(key)) return null;

            ApiReferenceBuildOutput reference;
            if (!references.TryGetValue(key, out reference))
            {
                return ApiReferenceBuildOutput.GetSpecNames(GetXref(key), supportedLanguages);
            }
            else
            {
                return reference.Spec;
            }
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
            var xref = $"<xref href=\"{HttpUtility.HtmlEncode(uid)}\"";
            if (!string.IsNullOrEmpty(fullName)) xref += $" fullName=\"{HttpUtility.HtmlEncode(fullName)}\"";
            if (!string.IsNullOrEmpty(name)) xref += $" name=\"{HttpUtility.HtmlEncode(name)}\"";
            xref += "/>";
            return xref;
        }
    }
}
