// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Docfx.Build.RestApi;

internal static partial class RestApiModelUtility
{
    [GeneratedRegex(@"\W")]
    private static partial Regex HtmlEncodeRegex();

    internal static string GetHtmlId(string id) => string.IsNullOrEmpty(id) ? null : HtmlEncodeRegex().Replace(id, "_");

    internal static string GenerateUid(params string[] segments) =>
        string.Join('/', segments.Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim('/')));

    internal static IEnumerable<T> MergeParameters<T>(IList<T> operationParameters, IList<T> pathParameters, Func<T, T, bool> equals)
    {
        if (pathParameters == null || pathParameters.Count == 0)
        {
            return operationParameters;
        }
        if (operationParameters == null || operationParameters.Count == 0)
        {
            return pathParameters;
        }

        return operationParameters.Union(pathParameters.Where(p => !operationParameters.Any(o => equals(p, o)))).ToList();
    }
}
