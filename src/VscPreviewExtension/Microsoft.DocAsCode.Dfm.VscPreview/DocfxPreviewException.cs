// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System;

    public class DocfxPreviewException : Exception
    {
        public DocfxPreviewException(): this("Error happens while handle VscPreview extension request")
        {
        }

        public DocfxPreviewException(string message) : base(message)
        {
        }

        public DocfxPreviewException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
