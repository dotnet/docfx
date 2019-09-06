// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    [JsonConverter(typeof(ShortHandConverter))]
    internal readonly struct DependencyPackageUrl
    {
        public readonly PackageUrl Url;

        public readonly bool InScope;

        public DependencyPackageUrl(string url)
            : this()
        {
            Url = new PackageUrl(url);
            InScope = false;
        }
    }
}
