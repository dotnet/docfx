// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.HtmlToPdf;
using Newtonsoft.Json;

namespace Docfx;

/// <summary>
/// PdfJsonConfig.
/// </summary>
/// <see href="https://dotnet.github.io/docfx/reference/docfx-json-reference.html#13-properties-for-pdf"/>
[Serializable]
internal class PdfJsonConfig : BuildJsonConfig
{
    /// <summary>
    /// Specifies the prefix of the generated PDF files.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// Specify the hostname to link not-in-TOC articles.
    /// </summary>
    [JsonProperty("host")]
    public string Host { get; set; }

    /// <summary>
    /// Specify the locale of the pdf file.
    /// </summary>
    [JsonProperty("locale")]
    public string Locale { get; set; }

    /// <summary>
    /// If specified, an appendices.pdf file is generated containing all the not-in-TOC articles.
    /// </summary>
    [JsonProperty("generatesAppendices")]
    public bool GeneratesAppendices { get; set; }

    /// <summary>
    /// Specify whether or not to generate external links for PDF.
    /// </summary>
    [JsonProperty("generatesExternalLink")]
    public bool GeneratesExternalLink { get; set; }

    /// <summary>
    /// If specified, the intermediate html files used to generate the PDF are not deleted after the PDF has been generated.
    /// </summary>
    [JsonProperty("keepRawFiles")]
    public bool KeepRawFiles { get; set; }

    /// <summary>
    /// Specify whether or not to exclude a table of contents.
    /// By default the value is false.
    /// </summary>
    [JsonProperty("excludeDefaultToc")]
    public bool ExcludeDefaultToc { get; set; }

    /// <summary>
    /// Specify the output folder for the raw files, if not specified,raw files will by
    /// default be saved to _raw subfolder under output folder if keepRawFiles is set to true
    /// </summary>
    [JsonProperty("rawOutputFolder")]
    public string RawOutputFolder { get; set; }

    /// <summary>
    /// Specify whether or not to exclude a table of contents.
    /// By default the value is false.
    /// </summary>
    [JsonProperty("excludedTocs")]
    public List<string> ExcludedTocs { get; set; }

    /// <summary>
    /// Specify the path for the css to generate pdf, default value is styles/default.css.
    /// </summary>
    [JsonProperty("css")]
    public string Css { get; set; }

    /// <summary>
    /// Specify the base path for ExternalLinkFormat.
    /// </summary>
    [JsonProperty("base")]
    public string Base { get; set; }

    /// <summary>
    /// Specify how to handle pages that fail to load: abort, ignore or skip(default abort)
    /// </summary>
    [JsonProperty("errorHandling")]
    public string ErrorHandling { get; set; }

    /// <summary>
    /// Specify options specific to the wkhtmltopdf tooling used by the pdf command.
    /// </summary>
    [JsonProperty("wkhtmltopdf")]
    public WkhtmltopdfJsonConfig Wkhtmltopdf { get; set; }

    /// <summary>
    /// Gets or sets the "Table of Contents" bookmark title.
    /// </summary>
    [JsonProperty("tocTitle")]
    public string TocTitle { get; set; } = "Table of Contents";

    /// <summary>
    /// Gets or sets the outline option.
    /// </summary>
    [JsonProperty("outline")]
    public OutlineOption Outline { get; set; } = OutlineOption.DefaultOutline;

    /// <summary>
    /// Gets or sets the cover page title.
    /// </summary>
    [JsonProperty("coverTitle")]
    public string CoverTitle { get; set; } = "Cover Page";

    /// <summary>
    /// Are input arguments set using command line
    /// </summary>
    [JsonProperty("noStdin")]
    public bool NoStdin { get; set; }
}
