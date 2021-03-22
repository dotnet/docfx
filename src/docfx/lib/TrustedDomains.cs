// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

                var protocol = split[0].ToLowerInvariant();
                var domain = split[1] != "*" ? split[1].ToLowerInvariant() : null;
                if (domain is null)
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

        public bool IsTrusted(ErrorBuilder errors, FilePath file, string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!_trustedDomains.TryGetValue(uri.Scheme, out var domains))
            {
                errors.Add(Errors.Content.DisallowedDomain(new(file), uri.GetLeftPart(UriPartial.Authority)));
                return false;
            }

            if (domains != null && !domains.Contains(uri.DnsSafeHost))
            {
                errors.Add(Errors.Content.DisallowedDomain(new(file), uri.GetLeftPart(UriPartial.Authority)));
                return false;
            }

            return true;
        }
    }
}
