// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Diagnostics;

    public class MarkdownTokenValidatorContext : IDisposable
    {
        [ThreadStatic]
        private static MarkdownTokenValidatorContext _current;

        private readonly IMarkdownRewriteEngine _rewriteEngine;

        internal MarkdownTokenValidatorContext(IMarkdownRewriteEngine rewriteEngine)
        {
            _rewriteEngine = rewriteEngine;
            Debug.Assert(_current == null, "Current context should be null.");
            _current = this;
        }

        public static IMarkdownRewriteEngine CurrentRewriteEngine => _current?._rewriteEngine;

        void IDisposable.Dispose()
        {
            _current = null;
        }
    }
}
