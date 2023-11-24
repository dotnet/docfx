// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.HtmlToPdf;
using Newtonsoft.Json;

namespace Docfx;

/// <summary>
/// PdfJsonConfig.
/// </summary>
/// <see href="https://dotnet.github.io/docfx/reference/docfx-json-reference.html#13-properties-for-pdf"/>
internal class PdfJsonConfig : BuildJsonConfig
{
    /// <summary>
    /// Specifies the prefix of the generated PDF files.
    /// </summary>
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Specify the hostname to link not-in-TOC articles.
    /// </summary>
    [JsonProperty("host")]
    [JsonPropertyName("host")]
    public string Host { get; set; }

    /// <summary>
    /// Specify the locale of the pdf file.
    /// </summary>
    [JsonProperty("locale")]
    [JsonPropertyName("locale")]
    public string Locale { get; set; }

    /// <summary>
    /// If specified, an appendices.pdf file is generated containing all the not-in-TOC articles.
    /// </summary>
    [JsonProperty("generatesAppendices")]
    [JsonPropertyName("generatesAppendices")]
    public bool GeneratesAppendices { get; set; }

    /// <summary>
    /// Specify whether or not to generate external links for PDF.
    /// </summary>
    [JsonProperty("generatesExternalLink")]
    [JsonPropertyName("generatesExternalLink")]
    public bool GeneratesExternalLink { get; set; }

    /// <summary>
    /// If specified, the intermediate html files used to generate the PDF are not deleted after the PDF has been generated.
    /// </summary>
    [JsonProperty("keepRawFiles")]
    [JsonPropertyName("keepRawFiles")]
    public bool KeepRawFiles { get; set; }

    /// <summary>
    /// Specify whether or not to exclude a table of contents.
    /// By default the value is false.
    /// </summary>
    [JsonProperty("excludeDefaultToc")]
    [JsonPropertyName("excludeDefaultToc")]
    public bool ExcludeDefaultToc { get; set; }

    /// <summary>
    /// Specify the output folder for the raw files, if not specified,raw files will by
    /// default be saved to _raw subfolder under output folder if keepRawFiles is set to true
    /// </summary>
    [JsonProperty("rawOutputFolder")]
    [JsonPropertyName("rawOutputFolder")]
    public string RawOutputFolder { get; set; }

    /// <summary>
    /// Specify whether or not to exclude a table of contents.
    /// By default the value is false.
    /// </summary>
    [JsonProperty("excludedTocs")]
    [JsonPropertyName("excludedTocs")]
    public List<string> ExcludedTocs { get; set; }

    /// <summary>
    /// Specify the path for the css to generate pdf, default value is styles/default.css.
    /// </summary>
    [JsonProperty("css")]
    [JsonPropertyName("css")]
    public string Css { get; set; }

    /// <summary>
    /// Specify the base path for ExternalLinkFormat.
    /// </summary>
    [JsonProperty("base")]
    [JsonPropertyName("base")]
    public string Base { get; set; }

    /// <summary>
    /// Specify how to handle pages that fail to load: abort, ignore or skip(default abort)
    /// </summary>
    [JsonProperty("errorHandling")]
    [JsonPropertyName("errorHandling")]
    public string ErrorHandling { get; set; }

    /// <summary>
    /// Specify options specific to the wkhtmltopdf tooling used by the pdf command.
    /// </summary>
    [JsonProperty("wkhtmltopdf")]
    [JsonPropertyName("wkhtmltopdf")]
    public WkhtmltopdfJsonConfig Wkhtmltopdf { get; set; }

    /// <summary>
    /// Gets or sets the "Table of Contents" bookmark title.
    /// </summary>
    [JsonProperty("tocTitle")]
    [JsonPropertyName("tocTitle")]
    public string TocTitle { get; set; } = "Table of Contents";

    /// <summary>
    /// Gets or sets the outline option.
    /// </summary>
    [JsonProperty("outline")]
    [JsonPropertyName("outline")]
    public OutlineOption Outline { get; set; } = OutlineOption.DefaultOutline;

    /// <summary>
    /// Gets or sets the cover page title.
    /// </summary>
    [JsonProperty("coverTitle")]
    [JsonPropertyName("coverTitle")]
    public string CoverTitle { get; set; } = "Cover Page";

    /// <summary>
    /// Are input arguments set using command line
    /// </summary>
    [JsonProperty("noStdin")]
    [JsonPropertyName("noStdin")]
    public bool NoStdin { get; set; }
}
