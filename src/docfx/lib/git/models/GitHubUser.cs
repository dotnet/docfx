// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs
{
    internal class GitHubUser
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Login { get; set; }

        public override string ToString() => Name;
    }
}
