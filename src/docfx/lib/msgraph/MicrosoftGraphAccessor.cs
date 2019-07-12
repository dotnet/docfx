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
        private const int UrlLengthLimit = 1000;

        private readonly IGraphServiceClient _msGraphClient;
        private readonly MicrosoftGraphAuthenticationProvider _microsoftGraphAuthenticationProvider;

        public MicrosoftGraphAccessor(string tenantId, string clientId, string clientSecret)
        {
            _microsoftGraphAuthenticationProvider = new MicrosoftGraphAuthenticationProvider(tenantId, clientId, clientSecret);
            _msGraphClient = new GraphServiceClient(_microsoftGraphAuthenticationProvider);
        }

        public async Task<(Dictionary<string, bool>, List<Error>)> ValidateAliases(HashSet<string> aliases)
        {
            var results = new Dictionary<string, bool>();
            var errors = new List<Error>();
            var fliterStrings = GetFliterStrings(aliases);

            if (fliterStrings.Count != 0)
            {
                var mailNicknames = await GetMailNicknames(fliterStrings, errors);

                foreach (var alias in aliases)
                {
                    results.Add(alias, mailNicknames.Any(mailNickname => string.Equals(mailNickname, alias, StringComparison.OrdinalIgnoreCase)));
                }
            }

            return (results, errors);
        }

        public void Dispose()
        {
            _microsoftGraphAuthenticationProvider?.Dispose();
        }

        private async Task<HashSet<string>> GetMailNicknames(List<string> fliterStrings, List<Error> errors)
        {
            var mailNicknames = new List<string>();

            foreach (var fliterString in fliterStrings)
            {
                try
                {
                    var options = new List<Option>
                    {
                        new QueryOption("$select", "mailNickname"),
                        new QueryOption("$filter", fliterString),
                    };

                    mailNicknames.AddRange((await RetryUtility.Retry(
                        () => _msGraphClient.Users.Request(options).GetAsync(),
                        ex => ex is ServiceException)).Select(user => user.MailNickname));
                }
                catch (Exception e)
                {
                    errors.Add(Errors.MicrosoftGraphApiFailed(e.Message));
                }
            }

            return mailNicknames.ToHashSet();
        }

        private List<string> GetFliterStrings(HashSet<string> aliases)
        {
            var fliterStrings = new List<string>();

            var fliterString = string.Empty;

            foreach (var alias in aliases)
            {
                fliterString = $"{fliterString}{(string.IsNullOrEmpty(fliterString) ? string.Empty : " or ")}mailNickname eq '{alias}'";

                if (fliterString.Length > UrlLengthLimit)
                {
                    fliterStrings.Add(fliterString);
                    fliterString = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(fliterString))
            {
                fliterStrings.Add(fliterString);
            }

            return fliterStrings;
        }
    }
}
