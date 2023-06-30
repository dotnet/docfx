// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.HtmlToPdf;

public class HtmlModel
{
    public string Title { get; set; }

    public string HtmlFilePath { get; set; }

    public string ExternalLink { get; set; }

    public IList<HtmlModel> Children { get; set; }
}
