// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Docfx.Build;
using Docfx.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Spectre.Console;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Actions;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Outline.Destinations;
using UglyToad.PdfPig.Writer;

using static Docfx.Build.HtmlTemplate;

#nullable enable

namespace Docfx.Pdf;

static class PdfBuilder
{
    class Outline
    {
        public string name { get; init; } = "";
        public string? href { get; init; }
        public Outline[]? items { get; init; }

        public bool pdf { get; init; }
        public string? pdfFileName { get; init; }
        public bool pdfTocPage { get; init; }
        public string? pdfCoverPage { get; init; }
    }

    public static Task Run(BuildJsonConfig config, string configDirectory, string? outputDirectory = null)
    {
        var outputFolder = Path.GetFullPath(Path.Combine(
            string.IsNullOrEmpty(outputDirectory) ? Path.Combine(configDirectory, config.Output ?? "") : outputDirectory,
            config.Dest ?? ""));

        return CreatePdf(outputFolder);
    }

    public static async Task CreatePdf(string outputFolder)
    {
        var pdfTocs = GetPdfTocs().ToDictionary(p => p.url, p => p.toc);
        if (pdfTocs.Count == 0)
            return;

        Program.Main(new[] { "install", "chromium" });

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        Uri? baseUrl = null;
        var pdfPageNumbers = new ConcurrentDictionary<string, Dictionary<Outline, int>>();

        using var app = builder.Build();
        app.UseServe(outputFolder);
        app.MapGet("/_pdftoc/{*url}", TocPage);
        await app.StartAsync();

        baseUrl = new Uri(app.Urls.First());

        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();

        await AnsiConsole.Progress().StartAsync(async progress =>
        {
            await Parallel.ForEachAsync(pdfTocs, async (item, _) =>
            {
                var (url, toc) = item;
                var task = progress.AddTask(url);
                var outputPath = Path.Combine(outputFolder, Path.GetDirectoryName(url) ?? "", toc.pdfFileName ?? Path.ChangeExtension(Path.GetFileName(url), ".pdf"));
                await CreatePdf(browser, task, new(baseUrl, url), toc, outputPath, pageNumbers => pdfPageNumbers[url] = pageNumbers);
                task.StopTask();
            });
        });

        IEnumerable<(string url, Outline toc)> GetPdfTocs()
        {
            var manifestPath = Path.Combine(outputFolder, "manifest.json");
            var manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(manifestPath));
            if (manifest is null)
                yield break;

            foreach (var file in manifest.Files)
            {
                if (file.Type != "Toc" || !file.Output.TryGetValue(".json", out var jsonOutput))
                    continue;

                var tocFile = Path.Combine(outputFolder, jsonOutput.RelativePath);
                if (!File.Exists(tocFile))
                    continue;

                var outline = JsonSerializer.Deserialize<Outline>(File.ReadAllBytes(tocFile));
                if (outline?.pdf is true)
                    yield return (jsonOutput.RelativePath, outline);
            }
        }

        IResult TocPage(string url)
        {
            var pageNumbers = pdfPageNumbers.TryGetValue(url, out var x) ? x : default;
            return Results.Content(TocHtmlTemplate(new Uri(baseUrl!, url), pdfTocs[url], pageNumbers).ToString(), "text/html");
        }
    }

    static async Task CreatePdf(IBrowser browser, ProgressTask task, Uri outlineUrl, Outline outline, string outputPath, Action<Dictionary<Outline, int>> updatePageNumbers)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), ".docfx", "pdf", "pages");
        Directory.CreateDirectory(tempDirectory);

        var pages = GetPages(outline).ToArray();
        if (pages.Length == 0)
            return;

        var pagesByNode = pages.ToDictionary(p => p.node);
        var pagesByUrl = new Dictionary<Uri, List<(Outline node, NamedDestinations namedDests)>>();
        var pageBytes = new Dictionary<Outline, byte[]>();
        var pageNumbers = new Dictionary<Outline, int>();
        var nextPageNumbers = new Dictionary<Outline, int>();
        var pageNumber = 1;
        var nextPageNumber = 1;

        var page = await browser.NewPageAsync(new() { UserAgent = "docfx/pdf" });

        // Make progress at 99% before merge PDF
        task.MaxValue = pages.Length + (pages.Length / 99.0);
        foreach (var (url, node) in pages)
        {
            var bytes = await CapturePdf(url, pageNumber);
            pageBytes[node] = bytes;

            using var document = PdfDocument.Open(bytes);

            var key = CleanUrl(url);
            if (!pagesByUrl.TryGetValue(key, out var dests))
                pagesByUrl[key] = dests = new();
            dests.Add((node, document.Structure.Catalog.NamedDestinations));

            pageNumbers[node] = pageNumber;
            pageNumber = document.NumberOfPages + 1;
            nextPageNumbers[node] = pageNumber;
            task.Value++;
        }

        await MergePdf();
        task.Value = task.MaxValue;

        async Task<byte[]> CapturePdf(Uri url, int startPageNumber)
        {
            var response = await page.GotoAsync(url.ToString());
            if (response is null || !response.Ok)
                throw new InvalidOperationException($"Failed to build PDF page [{response?.Status}]: {url}");

            await page.AddScriptTagAsync(new() { Content = EnsureHeadingAnchorScript });
            await page.AddScriptTagAsync(new() { Content = InsertHiddenPageScript(startPageNumber - 1) });
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await page.WaitForFunctionAsync("!window.docfx || window.docfx.ready");
            var bytes = await page.PdfAsync(new()
            {
                HeaderTemplate = "<span></span>",
                FooterTemplate = "<div style='width: 100%; font-size: 10px; padding: 0 40px'></span><span class='pageNumber' style='float: right'></span></div>",
                DisplayHeaderFooter = !IsTocPage(url),
            });

            return bytes;
        }

        IEnumerable<(Uri url, Outline node)> GetPages(Outline outline)
        {
            if (!string.IsNullOrEmpty(outline.pdfCoverPage))
            {
                var href = $"/{outline.pdfCoverPage}";
                yield return (new(outlineUrl, href), new() { href = href });
            }

            if (outline.pdfTocPage)
            {
                var href = $"/_pdftoc{outlineUrl.AbsolutePath}";
                yield return (new(outlineUrl, href), new() { href = href });
            }

            if (!string.IsNullOrEmpty(outline.href))
            {
                var url = new Uri(outlineUrl, outline.href);
                if (url.Host == outlineUrl.Host)
                    yield return (url, outline);
            }

            if (outline.items != null)
            {
                foreach (var item in outline.items)
                    foreach (var url in GetPages(item))
                        yield return url;
            }
        }

        async Task MergePdf()
        {
            using var output = File.Create(outputPath);
            using var builder = new PdfDocumentBuilder(output);

            builder.DocumentInformation = new()
            {
                Producer = $"docfx ({typeof(PdfBuilder).Assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version})",
            };

            var startPageNumber = 1;
            foreach (var (url, node) in pages)
            {
                var bytes = pageBytes[node];

                if (IsTocPage(url))
                {
                    // Refresh TOC page numbers
                    updatePageNumbers(pageNumbers);
                    bytes = await CapturePdf(url, startPageNumber);
                }

                using var document = PdfDocument.Open(bytes);
                for (var i = startPageNumber; i <= document.NumberOfPages; i++)
                {
                    builder.AddPage(document, i, CopyLink);
                }
                startPageNumber = document.NumberOfPages + 1;
            }

            builder.Bookmarks = new(CreateBookmarks(outline.items).ToArray());
        }

        PdfAction CopyLink(PdfAction action)
        {
            return action switch
            {
                GoToAction link => new GoToAction(new(link.Destination.PageNumber, link.Destination.Type, link.Destination.Coordinates)),
                UriAction url => HandleUriAction(url),
                _ => action,
            };

            PdfAction HandleUriAction(UriAction url)
            {
                if (!Uri.TryCreate(url.Uri, UriKind.Absolute, out var uri))
                    return url;

                if (!pagesByUrl.TryGetValue(CleanUrl(uri), out var pages))
                {
                    if (uri.Host == outlineUrl.Host && uri.Port == outlineUrl.Port)
                    {
                        // It is likely a 404 link if we are here
                        return new UriAction("");
                    }
                    return url;
                }

                if (!string.IsNullOrEmpty(uri.Fragment) && uri.Fragment.Length > 1)
                {
                    var name = uri.Fragment.Substring(1);
                    foreach (var (node, namedDests) in pages)
                    {
                        if (namedDests.TryGet(name, out var dest) && dest is not null)
                        {
                            return new GoToAction(dest);
                        }
                    }
                }

                return new GoToAction(new(pageNumbers[pages[0].node], ExplicitDestinationType.FitHorizontally, ExplicitDestinationCoordinates.Empty));
            }
        }

        static Uri CleanUrl(Uri url) => new UriBuilder(url) { Query = null, Fragment = null }.Uri;

        static bool IsTocPage(Uri url) => url.AbsolutePath.StartsWith("/_pdftoc/");

        IEnumerable<BookmarkNode> CreateBookmarks(Outline[]? items, int level = 0)
        {
            if (items is null)
                yield break;

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.href))
                {
                    yield return new DocumentBookmarkNode(
                        item.name, level,
                        new(nextPageNumber, ExplicitDestinationType.FitHorizontally, ExplicitDestinationCoordinates.Empty),
                        CreateBookmarks(item.items, level + 1).ToArray());
                    continue;
                }

                if (!pagesByNode.TryGetValue(item, out var page))
                {
                    yield return new UriBookmarkNode(
                        item.name, level,
                        new Uri(outlineUrl, item.href).ToString(),
                        CreateBookmarks(item.items, level + 1).ToArray());
                    continue;
                }

                if (!string.IsNullOrEmpty(item.name))
                {
                    nextPageNumber = nextPageNumbers[item];
                    yield return new DocumentBookmarkNode(
                        item.name, level,
                        new(pageNumbers[item], ExplicitDestinationType.FitHorizontally, ExplicitDestinationCoordinates.Empty),
                        CreateBookmarks(item.items, level + 1).ToArray());
                }
            }
        }
    }

    static HtmlTemplate TocHtmlTemplate(Uri baseUrl, Outline node, Dictionary<Outline, int>? pageNumbers)
    {
        return Html($"""
            <!DOCTYPE html>
            <html>
            <head>
              <link rel="stylesheet" href="/public/docfx.min.css">
              <link rel="stylesheet" href="/public/main.css">
            </head>
            <body class="pdftoc">
            <h1>Table of Contents</h1>
            <ul>{node.items?.Select(TocNode)}</ul>
            </body>
            </html>
            """);

        HtmlTemplate TocNode(Outline node) => string.IsNullOrEmpty(node.name) ? default : Html(
            $"""
            <li>
              {(string.IsNullOrEmpty(node.href) ? node.name : Html(
                  $"""
                  <a href='{(string.IsNullOrEmpty(node.href) ? null : new Uri(baseUrl, node.href))}'>{node.name}
                  {(pageNumbers?.TryGetValue(node, out var n) is true ? Html($"<span class='spacer'></span> <span class='page-number'>{n}</span>") : null)}
                  </a>
                  """))}
              {(node.items?.Length > 0 ? Html($"<ul>{node.items.Select(TocNode)}</ul>") : null)}
            </li>
            """);
    }

    /// <summary>
    /// Adds hidden links to headings to ensure Chromium saves heading anchors to named dests
    /// for cross page bookmark reference.
    /// </summary>
    static string EnsureHeadingAnchorScript =>
        """
        document.querySelectorAll('h1, h2, h3, h4, h5, h6').forEach(h => {
          if (h.id) {
            const a = document.createElement('a')
            a.href = '#' + h.id
            document.body.appendChild(a)
          }
        })
        """;

    /// <summary>
    /// Hack PDF start page number by inserting hidden pages
    /// https://github.com/puppeteer/puppeteer/issues/3383#issuecomment-428613372
    /// </summary>
    static string InsertHiddenPageScript(int n) =>
        $$"""
        window.pageStart = {{n}}
        const pages = Array.from({length: window.pageStart}).map(() => {
          const page = document.createElement('div')
          page.innerText = 'placeholder'
          page.style = "page-break-after: always; visibility: hidden;"
          return page
        });
        document.body.prepend(...pages)
        """;
}
