// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Microsoft.DocAsCode.Plugins;

    public interface IMarkdownEngine
    {
        string Markup(MarkdownContext context, MarkdownServiceParameters parameters);
        void ReportDependency(string file);
    }
}
