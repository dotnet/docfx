// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    public class ResourceInfo
    {
        public string Path { get; }
        public string Content { get; }
        public ResourceInfo(string path, string content)
        {
            Path = path;
            Content = content;
        }
    }
}
