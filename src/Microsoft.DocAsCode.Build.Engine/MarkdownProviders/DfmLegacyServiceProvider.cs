// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Composition;

    using Microsoft.DocAsCode.Plugins;

    [Export("dfm", typeof(IMarkdownServiceProvider))]
    [Export("dfm-2.13", typeof(IMarkdownServiceProvider))]
    public class DfmLegacyServiceProvider : DfmServiceProvider
    {
        protected override bool LegacyMode => true;
    }
}
