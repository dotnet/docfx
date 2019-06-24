// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Graph;

namespace Microsoft.Docs.Build
{
    internal class MicrosoftGraphAccessor : IDisposable
    {
        private readonly IGraphServiceClient _msGraphClient;
        private readonly MicrosoftGraphAuthenticationProvider _microsoftGraphAuthenticationProvider;

        public MicrosoftGraphAccessor(Config config)
        {
            _microsoftGraphAuthenticationProvider = new MicrosoftGraphAuthenticationProvider(config.JsonSchema.MicrosoftGraphTenantId, config.JsonSchema.MicrosoftGraphClientId, config.JsonSchema.MicrosoftGraphClientSecret);
            _msGraphClient = new GraphServiceClient(_microsoftGraphAuthenticationProvider);
        }

        public void Dispose()
        {
            _microsoftGraphAuthenticationProvider.Dispose();
        }
    }
}
