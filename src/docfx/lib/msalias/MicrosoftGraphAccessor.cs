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
        private const int MaxRetries = 4;
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(3);
        private readonly IGraphServiceClient _msGraphClient;
        private readonly MicrosoftGraphAuthenticationProvider _microsoftGraphAuthenticationProvider;
        private bool _connected = false;

        public MicrosoftGraphAccessor(string tenantId, string clientId, string clientSecret)
        {
            if (!string.IsNullOrEmpty(clientSecret))
            {
                _microsoftGraphAuthenticationProvider = new MicrosoftGraphAuthenticationProvider(tenantId, clientId, clientSecret);
                _msGraphClient = new GraphServiceClient(_microsoftGraphAuthenticationProvider);
                _connected = true;
            }
        }

        public async Task<(Error error, bool isValid)> ValidateAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias) || !_connected)
            {
                return (null, false);
            }

            var options = new List<Option>
            {
                new QueryOption("$select", "mailNickname"),
                new QueryOption("$filter", $"mailNickname eq '{alias}'"),
            };

            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    var users = await _msGraphClient.Users.Request(options).GetAsync();
                    return (null, users.Count > 0);
                }
                catch (Exception e)
                {
                    if (i == MaxRetries - 1)
                    {
                        return (Errors.GraphApiGetUsersFailed(e.Message), false);
                    }
                    else
                    {
                        await Task.Delay(_retryDelay);
                    }
                }
            }

            return (null, false);
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
