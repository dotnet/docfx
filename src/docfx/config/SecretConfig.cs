// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

/// <summary>
/// All secrets <b>MUST</b> go here otherwise they are print out to logs
/// </summary>
internal class SecretConfig
{
    /// <summary>
    /// Token that can be used to access the GitHub API.
    /// </summary>
    public string GithubToken { get; init; } = "";

    /// <summary>
    /// The base64 encoded client cert that can be used to access the Microsoft Graph API.
    /// </summary>
    public string MicrosoftGraphClientCertificate { get; init; } = "";

    /// <summary>
    /// Gets the authorization keys for required resources access
    /// </summary>
    public Dictionary<string, HttpConfig> Http { get; init; } = new Dictionary<string, HttpConfig>();

    public HttpConfig? GetHttpConfig(string url)
    {
        foreach (var (baseUrl, rule) in Http.OrderByDescending(pair => pair.Key, StringComparer.Ordinal))
        {
            if (url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return rule;
            }
        }
        return default;
    }
}
