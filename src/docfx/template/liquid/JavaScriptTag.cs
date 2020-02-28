// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using DotLiquid;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class JavaScriptTag : Tag
    {
        public override void Render(DotLiquid.Context context, TextWriter result)
        {
            result.Write($@"<script src=""{LiquidTemplate.GetThemeRelativePath(context, Markup)}"" ></script>");
        }
    }
}
