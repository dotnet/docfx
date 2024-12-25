// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Docfx.Build;
using Docfx.Common;
using Docfx.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Spectre.Console;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Actions;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Outline.Destinations;
using UglyToad.PdfPig.Writer;

using static Docfx.Build.HtmlTemplate;

#nullable enable

namespace Docfx.Pdf;

static class PdfBuilder
{
    private static readonly SearchValues<char> InvalidPathChars = SearchValues.Create(Path.GetInvalidPathChars());

    class Outline
    {
        public string name { get; init; } = "";
        public string? href { get; init; }
        public Outline[]? items { get; init; }

        public bool pdf { get; init; }
        public string? pdfFileName { get; init; }
        public bool pdfTocPage { get; init; }
        public bool pdfPrintBackground { get; init; }
        public string? pdfCoverPage { get; init; }

        public string? pdfHeaderTemplate { get; init; }
        public string? pdfFooterTemplate { get; init; }
    }

    public static Task Run(BuildJsonConfig config, string configDirectory, string? outputDirectory = null, CancellationToken cancellationToken = default)
    {
        var outputFolder = Path.GetFullPath(Path.Combine(
            string.IsNullOrEmpty(outputDirectory) ? Path.Combine(configDirectory, config.Output ?? "") : outputDirectory,
            config.Dest ?? ""));

        Logger.LogInfo($"Searching for manifest in {outputFolder}");
        return CreatePdf(outputFolder, cancellationToken);
    }

    public static async Task CreatePdf(string outputFolder, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var pdfTocs = GetPdfTocs().ToDictionary(p => p.url, p => p.toc);
        if (pdfTocs.Count == 0)
            return;

        PlaywrightHelper.EnsurePlaywrightNodeJsPath();

        Program.Main(["install", "chromium", "--only-shell"]);

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        Uri? baseUrl = null;
        var pdfPageNumbers = new ConcurrentDictionary<string, Dictionary<Outline, int>>();

        using var app = builder.Build();
        app.UseServe(outputFolder);
        app.MapGet("/_pdftoc/{*url}", TocPage);
        await app.StartAsync(cancellationToken);

        baseUrl = new Uri(app.Urls.First());

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        await using var context = await browser.NewContextAsync(new() { UserAgent = "docfx/pdf" });

        if (Environment.GetEnvironmentVariable("DOCFX_PDF_TIMEOUT") is { } timeout)
        {
            context.SetDefaultTimeout(int.Parse(timeout));
        }

        using var pageLimiter = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
        var pagePool = new ConcurrentBag<IPage>();
        var headerFooterTemplateCache = new ConcurrentDictionary<string, string>();
        var headerFooterPageCache = new ConcurrentDictionary<(string, string), Task<byte[]>>();

        var pdfBuildTask = AnsiConsole.Progress().StartAsync(async progress =>
        {
            await Parallel.ForEachAsync(pdfTocs, new ParallelOptions { CancellationToken = cancellationToken }, async (item, _) =>
            {
                var (url, toc) = item;
                var outputName = Path.Combine(Path.GetDirectoryName(url) ?? "", toc.pdfFileName ?? Path.ChangeExtension(Path.GetFileName(url), ".pdf"));
                var task = progress.AddTask(outputName);
                var pdfOutputPath = Path.Combine(outputFolder, outputName);

                await CreatePdf(
                    PrintPdf, PrintHeaderFooter, task, new(baseUrl, url), toc, outputFolder, pdfOutputPath,
                    pageNumbers => pdfPageNumbers[url] = pageNumbers,
                    cancellationToken);

                task.Value = task.MaxValue;
                task.StopTask();
            });
        });

        try
        {
            await pdfBuildTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!pdfBuildTask.IsCompleted)
            {
                // If pdf generation task is not completed.
                // Manually close playwright context/browser to immediately shutdown remaining tasks.
                await context.CloseAsync();
                await browser.CloseAsync();
                try
                {
                    await pdfBuildTask; // Wait AnsiConsole.Progress operation completed to output logs.
                }
                catch
                {
                    Logger.LogError($"PDF file generation is canceled by user interaction.");
                    return;
                }
            }
        }

        Logger.LogVerbose($"PDF done in {stopwatch.Elapsed}");
        return;

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
            var pageNumbers = pdfPageNumbers.GetValueOrDefault(url);
            return Results.Content(TocHtmlTemplate(new Uri(baseUrl!, url), pdfTocs[url], pageNumbers).ToString(), "text/html", Encoding.UTF8);
        }

        async Task<byte[]?> PrintPdf(Outline outline, Uri url)
        {
            await pageLimiter.WaitAsync(cancellationToken);
            var page = pagePool.TryTake(out var pooled) ? pooled : await context.NewPageAsync();

            try
            {
                var response = await page.GotoAsync(url.ToString(), new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                if (response?.Status is 404)
                    return null;

                if (response is null || !response.Ok)
                    throw new InvalidOperationException($"Failed to build PDF page [{response?.Status}]: {url}");

                try
                {
                    await page.AddScriptTagAsync(new() { Content = EnsureHeadingAnchorScript });
                    await page.WaitForFunctionAsync("!window.docfx || window.docfx.ready");
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
                catch (TimeoutException)
                {
                    Logger.LogWarning($"Timeout waiting for page to load, generated PDF page may be incomplete: {url}");
                }

                return await page.PdfAsync(new PagePdfOptions
                {
                    PreferCSSPageSize = true,
                    PrintBackground = outline.pdfPrintBackground,
                });
            }
            finally
            {
                pagePool.Add(page);
                pageLimiter.Release();
            }
        }

        Task<byte[]> PrintHeaderFooter(Outline toc, int pageNumber, int totalPages, Page contentPage)
        {
            var headerTemplate = ExpandTemplate(GetHeaderFooter(toc.pdfHeaderTemplate), pageNumber, totalPages);
            var footerTemplate = ExpandTemplate(GetHeaderFooter(toc.pdfFooterTemplate) ?? DefaultFooterTemplate, pageNumber, totalPages);

            return headerFooterPageCache.GetOrAdd((headerTemplate, footerTemplate), _ => PrintHeaderFooterCore());

            async Task<byte[]> PrintHeaderFooterCore()
            {
                await pageLimiter.WaitAsync();
                var page = pagePool.TryTake(out var pooled) ? pooled : await context.NewPageAsync();

                try
                {
                    await page.GotoAsync("about:blank");

                    var options = new PagePdfOptions
                    {
                        DisplayHeaderFooter = true,
                        HeaderTemplate = headerTemplate,
                        FooterTemplate = footerTemplate,
                    };

                    if (TryGetPlaywrightPageFormat(contentPage.Size, out var pageFormat))
                    {
                        options.Format = pageFormat;
                        options.Landscape = contentPage.Width > contentPage.Height;
                    }
                    else
                    {
                        var customPageSize = GetPageSizeSettings(contentPage);
                        options.Width = customPageSize.Width;
                        options.Height = customPageSize.Height;
                        options.Landscape = customPageSize.Landscape;
                    }

                    return await page.PdfAsync(options);
                }
                finally
                {
                    pagePool.Add(page);
                    pageLimiter.Release();
                }
            }

            static string ExpandTemplate(string? pdfTemplate, int pageNumber, int totalPages)
            {
                return (pdfTemplate ?? "")
                    .Replace("<span class='pageNumber'></span>", $"<span>{pageNumber}</span>")
                    .Replace("<span class=\"pageNumber\"></span>", $"<span>{pageNumber}</span>")
                    .Replace("<span class='totalPages'></span>", $"<span>{totalPages}</span>")
                    .Replace("<span class=\"totalPages\"></span>", $"<span>{totalPages}</span>");
            }

            string? GetHeaderFooter(string? template)
            {
                if (string.IsNullOrEmpty(template))
                    return template;

                // Check path chars. If it's contains HTML chars. Skip access to file content to optimmize performance
                if (template.AsSpan().ContainsAny(InvalidPathChars))
                    return template;

                return headerFooterTemplateCache.GetOrAdd(template, (_) =>
                {
                    // Note: This valueFactory might be called multiple times.
                    try
                    {
                        var path = Path.GetFullPath(Path.Combine(outputFolder, template));
                        if (!File.Exists(path))
                            return template;

                        var templateContent = File.ReadAllText(path);
                        return templateContent;
                    }
                    catch
                    {
                        return template;
                    }
                });
            }

        }
    }

    static async Task CreatePdf(
        Func<Outline, Uri, Task<byte[]?>> printPdf, Func<Outline, int, int, Page, Task<byte[]>> printHeaderFooter, ProgressTask task,
        Uri outlineUrl, Outline outline, string outputFolder, string pdfOutputPath, Action<Dictionary<Outline, int>> updatePageNumbers, CancellationToken cancellationToken)
    {
        var pages = GetPages(outline).ToArray();
        if (pages.Length == 0)
            return;

        var pageBytes = new Dictionary<Outline, byte[]>();

        // Make progress at 99% before merge PDF
        task.MaxValue = pages.Length + (pages.Length / 99.0);

        await Parallel.ForEachAsync(pages, new ParallelOptions { CancellationToken = cancellationToken }, async (item, _) =>
        {
            var (url, node) = item;
            if (await printPdf(outline, url) is { } bytes)
            {
                lock (pageBytes)
                    pageBytes[node] = bytes;
            }
            task.Increment(1);
        });

        var pagesByNode = pages.ToDictionary(p => p.node);
        var pagesByUrl = new Dictionary<Uri, List<(Outline node, NamedDestinations namedDests)>>();
        var pageNumbers = new Dictionary<Outline, int>();
        var numberOfPages = 0;

        foreach (var (url, node) in pages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!pageBytes.TryGetValue(node, out var bytes))
                continue;

            using var document = PdfDocument.Open(bytes);
            if (document.NumberOfPages is 0)
                continue;

            var key = CleanUrl(url);
            if (!pagesByUrl.TryGetValue(key, out var dests))
                pagesByUrl[key] = dests = [];
            dests.Add((node, document.Structure.Catalog.GetNamedDestinations()));

            pageBytes[node] = bytes;
            pageNumbers[node] = numberOfPages + 1;
            numberOfPages += document.NumberOfPages;
        }

        if (numberOfPages is 0)
            return;

        var producer = $"docfx ({typeof(PdfBuilder).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version})";

        using var output = File.Create(pdfOutputPath);
        using var builder = new PdfDocumentBuilder(output);

        builder.DocumentInformation = new() { Producer = producer };
        builder.Bookmarks = CreateBookmarks(outline.items);

        await MergePdf();
        return;

        IEnumerable<(Uri url, Outline node)> GetPages(Outline outline)
        {
            if (!string.IsNullOrEmpty(outline.pdfCoverPage))
            {
                var href = $"/{outline.pdfCoverPage}";
                yield return (new(outlineUrl, href), new() { href = href, pdfPrintBackground = outline.pdfPrintBackground });
            }

            if (outline.pdfTocPage)
            {
                var href = $"/_pdftoc{outlineUrl.AbsolutePath}";
                yield return (new(outlineUrl, href), new() { href = href, pdfPrintBackground = outline.pdfPrintBackground });
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
            var pageNumber = 0;
            var font = builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);

            foreach (var (url, node) in pages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!pageBytes.TryGetValue(node, out var bytes))
                    continue;

                var isCoverPage = IsCoverPage(url, outputFolder, outline.pdfCoverPage);

                var isTocPage = IsTocPage(url);
                if (isTocPage)
                {
                    // Refresh TOC page numbers
                    updatePageNumbers(pageNumbers);
                    bytes = await printPdf(outline, url);

                    if (bytes == null)
                        continue;
                }

                using var document = PdfDocument.Open(bytes);
                for (var i = 1; i <= document.NumberOfPages; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    pageNumber++;

                    var pageBuilder = builder.AddPage(document, i, x => CopyLink(node, x));

                    if (isCoverPage)
                        continue;

                    if (isTocPage)
                        continue;

                    var headerFooter = await printHeaderFooter(outline, pageNumber, numberOfPages, document.GetPage(i));
                    using var headerFooterDocument = PdfDocument.Open(headerFooter);

                    pageBuilder.NewContentStreamBefore();
                    pageBuilder.CurrentStream.Operations.Add(Push.Value);

                    // PDF produced by chromimum modifies global transformation matrix.
                    // Push and pop graphics state to fix graphics state
                    pageBuilder.CopyFrom(headerFooterDocument.GetPage(1));

                    pageBuilder.CurrentStream.Operations.Add(Pop.Value);
                }
            }
        }

        PdfAction CopyLink(Outline node, PdfAction action)
        {
            return action switch
            {
                GoToAction link => new GoToAction(new(pageNumbers[node] - 1 + link.Destination.PageNumber, link.Destination.Type, link.Destination.Coordinates)),
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
                            return new GoToAction(new(pageNumbers[node] - 1 + dest.PageNumber, dest.Type, dest.Coordinates));
                        }
                    }
                }

                return new GoToAction(new(pageNumbers[pages[0].node], ExplicitDestinationType.FitHorizontally, ExplicitDestinationCoordinates.Empty));
            }
        }

        static Uri CleanUrl(Uri url) => new UriBuilder(url) { Query = null, Fragment = null }.Uri;

        static bool IsCoverPage(Uri pageUri, string baseFolder, string? pdfCoverPage)
        {
            Debug.Assert(Path.IsPathFullyQualified(baseFolder));

            if (string.IsNullOrEmpty(pdfCoverPage))
                return false;

            string pagePath = pageUri.AbsolutePath.TrimStart('/');
            string covePagePath = PathUtility.MakeRelativePath(baseFolder, Path.GetFullPath(Path.Combine(baseFolder, pdfCoverPage)));

            return pagePath.Equals(covePagePath, GetStringComparison());
        }

        static bool IsTocPage(Uri url) => url.AbsolutePath.StartsWith("/_pdftoc/");

        Bookmarks CreateBookmarks(Outline[]? items)
        {
            var nextPageNumber = 1;
            var numbers = new Dictionary<Outline, int>();

            foreach (var node in Enumerate(items).Reverse())
            {
                if (pageNumbers.TryGetValue(node, out var pageNumber))
                    nextPageNumber = Math.Min(numberOfPages, pageNumber);
                else
                    pageNumber = nextPageNumber;
                numbers[node] = pageNumber;
            }

            return new(CreateBookmarksCore(items, 0).ToArray());

            IEnumerable<Outline> Enumerate(Outline[]? items)
            {
                if (items is null)
                    yield break;

                foreach (var item in items)
                {
                    yield return item;

                    foreach (var child in Enumerate(item.items))
                        yield return child;
                }
            }

            IEnumerable<BookmarkNode> CreateBookmarksCore(Outline[]? items, int level)
            {
                if (items is null)
                    yield break;

                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(item.name))
                        continue;

                    if (string.IsNullOrEmpty(item.href))
                    {
                        yield return new DocumentBookmarkNode(
                            item.name, level,
                            new(numbers[item], ExplicitDestinationType.FitHorizontally, ExplicitDestinationCoordinates.Empty),
                            CreateBookmarksCore(item.items, level + 1).ToArray());
                        continue;
                    }

                    if (!pagesByNode.TryGetValue(item, out var pageBuilder))
                    {
                        yield return new UriBookmarkNode(
                            item.name, level,
                            new Uri(outlineUrl, item.href).ToString(),
                            CreateBookmarksCore(item.items, level + 1).ToArray());
                        continue;
                    }

                    yield return new DocumentBookmarkNode(
                        item.name, level,
                        new(numbers[item], ExplicitDestinationType.FitHorizontally, ExplicitDestinationCoordinates.Empty),
                        CreateBookmarksCore(item.items, level + 1).ToArray());
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

    static string DefaultFooterTemplate =>
        """
        <div style="width: 100%; font-size: 12px;">
          <div style="float: right; padding: 0 2em">
            <span class="pageNumber"></span> / <span class="totalPages"></span>
          </div>
        </div>
        """;

    /// <summary>
    /// Gets playwright page format from PdfPig's PageSize.
    /// </summary>
    private static bool TryGetPlaywrightPageFormat(PageSize pageSize, out string? pageFormat)
    {
        // List of supported formats: https://playwright.dev/dotnet/docs/api/class-page#page-pdf
        switch (pageSize)
        {
            case PageSize.Letter:
            case PageSize.Legal:
            case PageSize.Tabloid:
            case PageSize.Ledger:
            case PageSize.A0:
            case PageSize.A1:
            case PageSize.A2:
            case PageSize.A3:
            case PageSize.A4:
            case PageSize.A5:
            case PageSize.A6:
                pageFormat = pageSize.ToString();
                return true;

            // Following format is not supported format by playwright.
            // It need to use Width/Height settings.
            case PageSize.A7:
            case PageSize.A8:
            case PageSize.A9:
            case PageSize.A10:
            case PageSize.Custom:
            case PageSize.Executive:
            default:
                pageFormat = null;
                return false;
        }
    }

    /// <summary>
    /// Gets page size settings from PdfPig's Page object.
    /// </summary>
    private static (string Width, string Height, bool Landscape) GetPageSizeSettings(Page contentPage)
    {
        var isLandscape = contentPage.Width > contentPage.Height;
        var width = getMillimeter(contentPage.Width);
        var height = getMillimeter(contentPage.Height);

        return isLandscape
            ? (height, width, true) // On Landscape mode. It need to swap width/height.
            : (width, height, false);

        // Gets millimeter string representation from `pt` value.
        static string getMillimeter(double pt)
        {
            const double MillimeterPerInch = 25.4d;
            const double Dpi = 72d; // Use Default DPI of PDF.
            return $"{Math.Round(pt * MillimeterPerInch / Dpi)}mm";
        }
    }

    // Gets StringComparison instance for path string.
    private static StringComparison GetStringComparison()
    {
        return PathUtility.IsPathCaseInsensitive()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }
}
