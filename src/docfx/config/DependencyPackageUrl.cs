// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    [JsonConverter(typeof(ShortHandConverter))]
    internal class DependencyPackageUrl : PackageUrl
    {
        /// <summary>
        /// Indicate the dependency repository may be added to <see cref="BuildScope"/> and treated as inScope.
        /// </summary>
        public bool BuildFiles { get; set; }

        public DependencyPackageUrl()
            : base()
        {
        }

        public DependencyPackageUrl(string url)
            : base(url)
        {
        }
    }
}
