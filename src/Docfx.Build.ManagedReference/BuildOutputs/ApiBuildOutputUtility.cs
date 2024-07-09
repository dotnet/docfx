// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Web;

namespace Docfx.Build.ManagedReference.BuildOutputs;

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
            var value = values.GetValueOrDefault(language, defaultValue);

            // TODO: Sometimes output contains undeterministic \n\n sequences
            value = value.Replace("\n\n", "\n");

            result.Add(new ApiLanguageValuePair
            {
                Language = language,
                Value = value,
            });
        }

        return result;
    }

    public static string GetXref(string uid, string text = null)
    {
        var sb = new StringBuilder();
        sb.Append("<xref uid=\"")
            .Append(HttpUtility.HtmlEncode(uid))
            .Append('"');
        if (!string.IsNullOrEmpty(text))
        {
            sb.Append(" text=\"")
                .Append(HttpUtility.HtmlEncode(text))
                .Append('"');
        }
        else
        {
            sb.Append(" displayProperty=\"name\"");
        }
        sb.Append("/>");
        return sb.ToString();
    }

    public static string GetHref(string url, string altText = null) =>
        $@"<span><a href=""{url}"">{HttpUtility.HtmlEncode(altText ?? string.Empty)}</a></span>";
}
