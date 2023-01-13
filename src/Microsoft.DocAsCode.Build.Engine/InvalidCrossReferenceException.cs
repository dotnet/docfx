// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Microsoft.DocAsCode.Plugins;

    public class InvalidCrossReferenceException : DocumentException
    {
        public XRefDetails XRefDetails { get; }

        public InvalidCrossReferenceException(XRefDetails xrefDetails) : base()
        {
            XRefDetails = xrefDetails;
        }
    }
}
