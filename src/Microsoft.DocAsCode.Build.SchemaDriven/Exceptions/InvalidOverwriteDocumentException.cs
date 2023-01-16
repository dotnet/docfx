// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    public class InvalidOverwriteDocumentException : DocfxException
    {
        public InvalidOverwriteDocumentException(string message) : base(message)
        {
        }
    }
}
