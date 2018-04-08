// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs
{
    /// <summary>
    /// The github user information
    /// </summary>
    public class GitHubUser
    {
        /// <summary>
        /// Gets or sets github user name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets github user email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets github login
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Gets github user string representation
        /// </summary>
        /// <returns>The github user string representation</returns>
        public override string ToString() => Name;
    }
}
