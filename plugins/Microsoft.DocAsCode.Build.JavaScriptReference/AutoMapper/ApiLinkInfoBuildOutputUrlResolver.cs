﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System.Web;

    using AutoMapper;

    public class ApiHrefLinkInfoBuildOutputUrlResolver : IValueResolver<LinkInfo, ApiLinkInfoBuildOutput, string>
    {
        public string Resolve(LinkInfo source, ApiLinkInfoBuildOutput destination, string destMember, ResolutionContext context)
        {
            var href = $"<span><a href=\"{source.LinkId}\">";
            if (!string.IsNullOrEmpty(source.AltText))
            {
                href += HttpUtility.HtmlEncode(source.AltText);
            }
            href += "</a></span>";
            return href;
        }
    }
}
