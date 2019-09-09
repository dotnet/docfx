// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal interface IXrefResolver
    {
        (Error error, string href, string displayName, FilePath declaringFile) ResolveXref(
            SourceInfo<string> href, FilePath referencingFile);

        (Error error, IXrefSpec xrefSpec) ResolveXrefSpec(SourceInfo<string> uid, FilePath referencingFile);
    }
}
