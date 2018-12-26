// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public enum LocalizationMapping
    {
        /// <summary>
        /// localization files are stored in an **independent repository** per locale but keep the **same folder structure**
        /// example:
        /// source repo         locale        localization repo
        /// dotnet/docfx        zh-cn         dotnet/docfx.zh-cn
        /// dotnet/docfx        de-de         dotnet/docfx.de-de
        /// </summary>
        Repository,

        /// <summary>
        /// localization files are stored in the **same repository** with source files but under different **locale folder**
        /// example:
        /// source file         -->         localization files
        /// /readme.md          -->         /localization/zh-cn/readme.md
        /// /files/a.md         -->         /localization/de-de/files/a.md
        /// </summary>
        /// TODO: support build from localization folder directly?
        Folder,

        /// <summary>
        /// localization files are stored in ONE **different repository** for **all locales** under different **locale folder**
        /// repo mapping example:
        /// source repo         locale        localization repo
        /// dotnet/docfx        zh-cn         dotnet/docfx.localization
        /// dotnet/docfx        de-de         dotnet/docfx.localization
        /// folder mapping example:
        /// source repo         -->           localization repo
        /// /readme.md          -->           /zh-cn/readme.md
        /// /files/a.md         -->           /zh-cn/files/a.md
        /// </summary>
        /// TODO: remove this mapping
        RepositoryAndFolder,

        /// <summary>
        /// localization files are stored in ONE **different repository** for **all locales** under different **locale branch**
        /// repo mapping example:
        /// source repo         locale        localization repo
        /// dotnet/docfx        zh-cn         dotnet/docfx.localization
        /// dotnet/docfx        de-de         dotnet/docfx.localization
        /// branch mapping example:
        /// source branch         -->           localization branch
        /// #master               -->           #zh-cn-master
        /// #live                 -->           #zh-cn-live
        /// </summary>
        /// TODO:
        /// 1. branch convention change to {branch}.{locale}
        /// 2. repo name convention change to {name}.loc
        Branch,
    }
}
