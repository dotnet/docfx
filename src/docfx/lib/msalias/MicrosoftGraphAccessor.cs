// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace Microsoft.Docs.Build
{
    internal class MicrosoftGraphAccessor : IDisposable
    {
        private readonly IGraphServiceClient _msGraphClient;
        private readonly MicrosoftGraphAuthenticationProvider _microsoftGraphAuthenticationProvider;

        public bool Connected { get; } = true;

        public MicrosoftGraphAccessor(string tenantId, string clientId, string clientSecret)
        {
            try
            {
                _microsoftGraphAuthenticationProvider = new MicrosoftGraphAuthenticationProvider(tenantId, clientId, clientSecret);
                _msGraphClient = new GraphServiceClient(_microsoftGraphAuthenticationProvider);
            }
            catch
            {
                Connected = false;
            }
        }

        public async Task<bool> ValidateAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return false;
            }

            var options = new List<Option>
            {
                new QueryOption("$select", "mailNickname"),
                new QueryOption("$filter", $"mailNickname eq '{alias}'"),
            };

            var users = await _msGraphClient.Users.Request(options).GetAsync();
            return users.Count > 0;
        }

        public void Dispose()
        {
            if (_microsoftGraphAuthenticationProvider != null)
            {
                _microsoftGraphAuthenticationProvider.Dispose();
            }
        }
    }
}
