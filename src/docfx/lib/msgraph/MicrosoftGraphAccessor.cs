// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace Microsoft.Docs.Build
{
    internal class MicrosoftGraphAccessor : IDisposable
    {
        private readonly IGraphServiceClient _msGraphClient;
        private readonly MicrosoftGraphAuthenticationProvider _microsoftGraphAuthenticationProvider;

        public MicrosoftGraphAccessor(string tenantId, string clientId, string clientSecret)
        {
            _microsoftGraphAuthenticationProvider = new MicrosoftGraphAuthenticationProvider(tenantId, clientId, clientSecret);
            _msGraphClient = new GraphServiceClient(_microsoftGraphAuthenticationProvider);
        }

        public async Task<(Dictionary<string, bool>, Error)> ValidateAliases(HashSet<string> aliases)
        {
            var results = new Dictionary<string, bool>();
            var fliterString = GetFliterString(aliases);

            if (!string.IsNullOrEmpty(fliterString))
            {
                var options = new List<Option>
                {
                    new QueryOption("$select", "mailNickname"),
                    new QueryOption("$filter", fliterString),
                };

                try
                {
                    var userMailNicknames = (await RetryUtility.Retry(
                        () => _msGraphClient.Users.Request(options).GetAsync(),
                        ex => ex is ServiceException)).Select(user => user.MailNickname);

                    foreach (var alias in aliases)
                    {
                        results.Add(alias, userMailNicknames.Any(mailNickname => string.Equals(mailNickname, alias, StringComparison.OrdinalIgnoreCase)));
                    }

                    return (results, null);
                }
                catch (Exception e)
                {
                    return (results, Errors.MicrosoftGraphApiFailed(e.Message));
                }
            }

            return (results, null);
        }

        public void Dispose()
        {
            _microsoftGraphAuthenticationProvider?.Dispose();
        }

        private string GetFliterString(HashSet<string> aliases)
        {
            var fliterString = string.Empty;

            foreach (var alias in aliases)
            {
                fliterString = $"{fliterString}{(string.IsNullOrEmpty(fliterString) ? string.Empty : " or ")}mailNickname eq '{alias}'";
            }

            return fliterString;
        }
    }
}
