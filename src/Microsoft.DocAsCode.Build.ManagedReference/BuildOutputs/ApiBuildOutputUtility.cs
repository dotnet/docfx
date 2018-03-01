// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System.Collections.Generic;
    using System.Text;
    using System.Web;

    public static class ApiBuildOutputUtility
    {
        public static ApiReferenceBuildOutput GetReferenceViewModel(string key, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            if (!references.TryGetValue(key, out ApiReferenceBuildOutput arbo))
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
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            var result = ApiNames.FromUid(key);
            if (references.TryGetValue(key, out ApiReferenceBuildOutput arbo))
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
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            if (!references.TryGetValue(key, out ApiReferenceBuildOutput reference))
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
            if (string.IsNullOrEmpty(defaultValue) ||
                supportedLanguages == null ||
                supportedLanguages.Length == 0)
            {
                return null;
            }
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
            var sb = new StringBuilder();
            sb.Append("<xref uid=\"")
                .Append(HttpUtility.HtmlEncode(uid))
                .Append("\"");
            if (!string.IsNullOrEmpty(text))
            {
                sb.Append(" text=\"")
                    .Append(HttpUtility.HtmlEncode(text))
                    .Append("\"");
            }
            else
            {
                sb.Append(" displayProperty=\"name\"");
            }
            if (!string.IsNullOrEmpty(alt))
            {
                sb.Append(" alt=\"")
                    .Append(HttpUtility.HtmlEncode(alt))
                    .Append("\"");
            }
            else
            {
                sb.Append(" altProperty=\"fullName\"");
            }
            sb.Append("/>");
            return sb.ToString();
        }

        public static string GetHref(string url, string altText = null) =>
            $@"<span><a href=""{url}"">{
                HttpUtility.HtmlEncode(altText ?? string.Empty)
                }</a></span>";
    }
}
