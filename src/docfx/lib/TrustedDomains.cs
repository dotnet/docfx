// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    [JsonConverter(typeof(ShortHandConverter))]
    internal class TrustedDomains
    {
        private static readonly string[] s_splitStrings = new[] { ":", "//" };

        private readonly Dictionary<string, HashSet<string>?> _trustedDomains = new(StringComparer.OrdinalIgnoreCase);

        public TrustedDomains(string[] trustedDomains)
        {
            foreach (var trustedDomain in trustedDomains)
            {
                var split = trustedDomain.Split(s_splitStrings, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length != 2)
                {
                    throw new ArgumentException("Trusted domain must contain both protocol and domain, use * to match all domains.");
                }

                var protocol = split[0];
                var domain = split[1];
                if (domain == "*")
                {
                    _trustedDomains[protocol] = null;
                }
                else
                {
                    if (!_trustedDomains.TryGetValue(protocol, out var domains))
                    {
                        _trustedDomains[protocol] = domains = new(StringComparer.OrdinalIgnoreCase);
                    }

                    if (domains != null)
                    {
                        domains.Add(domain);
                    }
                }
            }
        }

        public bool IsTrusted(string url, [NotNullWhen(false)] out string? untrustedDomain)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                untrustedDomain = url;
                return false;
            }

            // Special case for links without protocol: '//codepen.io'. Uri treats them as files.
            if (uri.Scheme == Uri.UriSchemeFile && url.StartsWith("//"))
            {
                if (IsTrusted("https", uri.DnsSafeHost))
                {
                    untrustedDomain = null;
                    return true;
                }

                untrustedDomain = $"//{uri.DnsSafeHost}";
                return false;
            }

            if (IsTrusted(uri.Scheme, uri.DnsSafeHost))
            {
                untrustedDomain = null;
                return true;
            }

            untrustedDomain = uri.GetLeftPart(UriPartial.Authority);
            if (string.IsNullOrEmpty(untrustedDomain))
            {
                untrustedDomain = uri.GetLeftPart(UriPartial.Scheme);
            }

            return false;
        }

        private bool IsTrusted(string protocol, string domain)
        {
            if (_trustedDomains.TryGetValue(protocol, out var domains))
            {
                if (domains is null || domains.Contains(domain))
                {
                    return true;
                }
            }

            if (protocol == "https")
            {
                return IsTrusted("http", domain);
            }

            return false;
        }
    }
}
