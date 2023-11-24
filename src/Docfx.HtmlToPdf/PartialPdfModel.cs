// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.HtmlToPdf;

public class PartialPdfModel
{
    public string FilePath { get; set; }

    public int NumberOfPages { get; set; }

    public int? PageNumber { get; set; }
}
