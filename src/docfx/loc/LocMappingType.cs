// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public enum LocMappingType
    {
        /// <summary>
        /// loc files are stored in an **independent repository** per locale but keep the **same folder structure**
        /// example:
        /// source repo         locale        loc repo
        /// dotnet/docfx        zh-cn         dotnet/docfx.zh-cn
        /// dotnet/docfx        de-de         dotnet/docfx.de-de
        /// </summary>
        Repository,

        /// <summary>
        /// loc files are stored in the **same repository** with source files but under different **locale folder**
        /// example:
        /// source file         -->         loc files
        /// /readme.md          -->         /localization/zh-cn/readme.md
        /// /files/a.md         -->         /localization/de-de/files/a.md
        /// </summary>
        Folder,

        /// <summary>
        /// loc files are stored in ONE **different repository** for **all locales** under different **locale folder**
        /// repo mapping example:
        /// source repo         locale        loc repo
        /// dotnet/docfx        zh-cn         dotnet/docfx.localization
        /// dotnet/docfx        de-de         dotnet/docfx.localization
        /// folder mapping example:
        /// source repo         -->           loc repo
        /// /readme.md          -->           /zh-cn/readme.md
        /// /files/a.md         -->           /zh-cn/files/a.md
        /// </summary>
        RepositoryAndFolder,
    }
}
