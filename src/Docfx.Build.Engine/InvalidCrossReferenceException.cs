// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Build.Engine;

public class InvalidCrossReferenceException : DocumentException
{
    public XRefDetails XRefDetails { get; }

    public InvalidCrossReferenceException(XRefDetails xrefDetails) : base()
    {
        XRefDetails = xrefDetails;
    }
}
