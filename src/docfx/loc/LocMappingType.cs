// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public enum LocMappingType
    {
        Repository, // loc files are stored in an independent repository like dotnet/docfx.zh-cn per locale
        Folder, // loc files are stored in the same repository with source files but in different folder like dotnet/docfx/zh-cn
        RepositoryAndFolder, // loc files are stored in a different repository for all locales like dotnet/docfx.localization/zh-cn
        SideBySide, // loc files are stored in the same repository with source files but with different file name like dotnet/docfx/readme.zh-cn.md
    }
}
