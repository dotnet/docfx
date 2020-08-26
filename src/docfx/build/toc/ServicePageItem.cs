// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class ServicePageItem
    {
        public SourceInfo<string?> Name { get; private set; }

        public SourceInfo<string?> Href { get; private set; }

        public SourceInfo<string?> Uid { get; private set; }

        public ServicePageItem(SourceInfo<string?> name, SourceInfo<string?> href, SourceInfo<string?> uid)
        {
            Name = name;
            Href = href;
            Uid = uid;
        }
    }
}
