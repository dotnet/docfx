// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;
internal class HostNameUtility
{
    /// <summary>
    /// check if the hostname is in mapping, if yes, replace with corresponding value
    /// </summary>
    public static string ReplaceHostName(string hostName, IReadOnlyDictionary<string, string>? hostNameMapping)
    {
        if (hostNameMapping is null || string.IsNullOrEmpty(hostName))
        {
            return hostName;
        }

        return hostNameMapping.GetValueOrDefault(hostName, hostName);
    }

    /// <summary>
    /// check if the hostname of the url is in mapping, if yes, replace with corresponding value
    /// </summary>
    public static string? ReplaceHostForUrl(string? url, IReadOnlyDictionary<string, string>? hostNameMapping)
    {
        if (hostNameMapping is null || string.IsNullOrEmpty(url))
        {
            return url;
        }

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            var uri = new Uri(url);
            if (hostNameMapping.ContainsKey(uri.Host))
            {
                return new UriBuilder(uri)
                {
                    Host = hostNameMapping[uri.Host],
                }.Uri.AbsoluteUri;
            }
        }

        return url;
    }
}
