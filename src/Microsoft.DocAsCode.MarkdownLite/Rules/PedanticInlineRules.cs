// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Pedantic Inline Grammar
    /// </summary>
    public class PedanticInlineRules : InlineRules
    {

        public override Regex Strong { get { return Regexes.Inline.Pedantic.Strong; } }

        public override Regex Em { get { return Regexes.Inline.Pedantic.Em; } }

    }
}
