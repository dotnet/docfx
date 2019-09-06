// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal interface IMetadataProvider
    {
        (Error[] errors, InputMetadata metadata) GetMetadata(FilePath file);

        bool TryGetHtmlMetaName(string metadataName, out string htmlMetaName);
    }
}
