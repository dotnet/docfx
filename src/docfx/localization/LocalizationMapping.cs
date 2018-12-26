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
        /// localization files are stored in ONE **different repository** for **all locales** under different **locale branch**
        /// repo mapping example:
        /// source repo         locale        localization repo
        /// dotnet/docfx        zh-cn         dotnet/docfx.loc
        /// dotnet/docfx        de-de         dotnet/docfx.loc
        /// branch mapping example:
        /// source branch         -->           localization branch
        /// #master               -->           #master.zh-cn
        /// #live                 -->           #live.zh-cn
        /// #live       -> bilingual ->         #live-sxs.zh-cn
        /// </summary>
        /// TODO:
        /// 1. branch convention change to {branch}.{locale}
        /// 2. repo name convention change to {name}.loc
        Branch,
    }
}
