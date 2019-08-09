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

        public MicrosoftGraphAccessor(string tenantId, string clientId, string clientSecret)
        {
            _microsoftGraphAuthenticationProvider = new MicrosoftGraphAuthenticationProvider(tenantId, clientId, clientSecret);
            _msGraphClient = new GraphServiceClient(_microsoftGraphAuthenticationProvider);
        }

        public async Task<(Error error, bool isValid)> ValidateAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return (null, false);
            }

            var options = new List<Option>
            {
                new QueryOption("$select", "mailNickname"),
                new QueryOption("$filter", $"mailNickname eq '{alias}'"),
            };

            try
            {
                var users = await RetryUtility.Retry(
                    () => _msGraphClient.Users.Request(options).GetAsync(),
                    ex => ex is ServiceException);

                return (null, users != null ? users.Count > 0 : false);
            }
            catch (Exception e)
            {
                return (Errors.MicrosoftGraphApiFailed(e.Message), false);
            }
        }

        public void Dispose()
        {
            _microsoftGraphAuthenticationProvider?.Dispose();
        }
    }
}
