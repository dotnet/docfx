// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine
{
    using System.Composition;

    using Microsoft.DocAsCode.Plugins;

    [Export("markdig", typeof(IMarkdownServiceProvider))]
    public class MarkdigServiceProvider : IMarkdownServiceProvider
    {
        [Import]
        public ICompositionContainer Container { get; set; }

        public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
        {
            return new MarkdigMarkdownService(parameters, Container);
        }
    }
}