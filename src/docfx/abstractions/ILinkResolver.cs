// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal interface ILinkResolver
    {
        (Error error, string content, FilePath declaringFile) ResolveContent(
            SourceInfo<string> href, FilePath referencingFile, DependencyType dependencyType = DependencyType.Inclusion);

        (Error error, string url, FilePath declaringFile) ResolveLink(SourceInfo<string> href, FilePath referencingFile);
    }
}
