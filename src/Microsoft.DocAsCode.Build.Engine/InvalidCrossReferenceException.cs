// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.Engine;

public class InvalidCrossReferenceException : DocumentException
{
    public XRefDetails XRefDetails { get; }

    public InvalidCrossReferenceException(XRefDetails xrefDetails) : base()
    {
        XRefDetails = xrefDetails;
    }
}
