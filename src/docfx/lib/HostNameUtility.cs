// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;
internal class HostNameUtility
{
    public static string RebrandHostName(string hostName, Dictionary<string, string>? hostNameMapping)
    {
        if (hostNameMapping is null || string.IsNullOrEmpty(hostName))
        {
            return hostName;
        }

        if (hostNameMapping.ContainsKey(hostName))
        {
            return hostNameMapping[hostName];
        }
        return hostName;
    }

    public static string? RebrandUrl(string? url, Dictionary<string, string>? hostNameMapping)
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
                    Host = RebrandHostName(uri.Host, hostNameMapping),
                }.Uri.AbsoluteUri;
            }
        }

        return url;
    }
}
