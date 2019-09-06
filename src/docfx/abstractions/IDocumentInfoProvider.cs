// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal interface IDocumentInfoProvider
    {
        SourceInfo<string> GetSchemaName(FilePath file);

        string GetSiteUrl(FilePath file);

        string GetSitePath(FilePath file);

        (string id, string versionIndependentId) GetDocumentId(FilePath file);

        string GetOutputPath(FilePath file);

        bool IsExperimental(FilePath file);
    }
}
