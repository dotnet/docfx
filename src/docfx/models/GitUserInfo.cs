// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class GitUserInfo
    {
        public string ProfileUrl { get; set; }

        public string DisplayName { get; set; }

        public string Id { get; set; }
    }
}
