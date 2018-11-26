// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.IO;

    public interface IResourceFileReader
    {
        IEnumerable<string> Names { get; }

        string GetResource(string name);

        IEnumerable<ResourceInfo> GetResources(string selector);

        IEnumerable<KeyValuePair<string, Stream>> GetResourceStreams(string selector);

        Stream GetResourceStream(string name);
    }
}
