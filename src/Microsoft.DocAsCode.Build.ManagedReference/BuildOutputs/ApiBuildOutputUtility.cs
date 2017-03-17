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

            ApiReferenceBuildOutput arbo;
            if (!references.TryGetValue(key, out arbo))
            {
                arbo = new ApiReferenceBuildOutput
                {
                    Spec = ApiReferenceBuildOutput.GetSpecNames(GetXref(key), supportedLanguages),
                };
            }
            else
            {
                arbo.Expand(references, supportedLanguages);
            }

            return arbo;
        }

        public static ApiReferenceBuildOutput GetReferenceViewModel(string key, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages, int index)
        {
            var arbo = GetReferenceViewModel(key, references, supportedLanguages);
            arbo.Index = index;
            return arbo;
        }

        public static ApiNames GetApiNames(string key, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (string.IsNullOrEmpty(key)) return null;

            var result = ApiNames.FromUid(key);
            ApiReferenceBuildOutput arbo;
            if (references.TryGetValue(key, out arbo))
            {
                result.Definition = arbo.Definition;
                result.Name = arbo.Name;
                result.NameWithType = arbo.NameWithType;
                result.FullName = arbo.FullName;
                result.Spec = arbo.Spec;
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

        public static string GetXref(string uid, string text = null, string alt = null)
        {
            var result = $"<xref uid=\"{HttpUtility.HtmlEncode(uid)}\"";
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

        public static string GetHref(string url, string altText = null)
        {
            var href = $"<span><a href=\"{url}\">";
            if (!string.IsNullOrEmpty(altText)) href += HttpUtility.HtmlEncode(altText);
            href += "</a></span>";
            return href;
        }
    }
}
