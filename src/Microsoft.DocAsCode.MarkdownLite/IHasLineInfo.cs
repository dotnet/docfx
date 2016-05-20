// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public interface IHasLineInfo
    {
        /// <summary>
        /// The start line of current token.
        /// </summary>
        int LineNumber { get; set; }

        /// <summary>
        /// The file of current token.
        /// </summary>
        string File { get; set; }
    }
}
