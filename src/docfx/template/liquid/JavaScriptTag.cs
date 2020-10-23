// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using DotLiquid;

namespace Microsoft.Docs.Build
{
    internal class JavaScriptTag : Tag
    {
        public override void Render(DotLiquid.Context context, TextWriter result)
        {
            var templateBasePath = (string?)context.Registers["template_base_path"];
            result.Write($@"<script src=""{LiquidTemplate.GetThemeRelativePath(templateBasePath, Markup)}"" ></script>");
        }
    }
}
