// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class ContributionConfig
    {
        /// <summary>
        /// Determine whether to show links for contribution.
        /// </summary>
        public readonly bool Enabled = true;

        /// <summary>
        /// The path of user profile cache, used for generating authoer and contributors.
        /// </summary>
        public readonly string UserProfileCachePath = string.Empty;
    }
}
