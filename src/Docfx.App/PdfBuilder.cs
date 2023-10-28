// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Docfx.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Spectre.Console;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Actions;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Outline.Destinations;
using UglyToad.PdfPig.Writer;

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
        public string? pdfMargin { get; init; }
        public bool pdfPrintBackground { get; init; }
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
        var pdfTocs = GetPdfTocs().ToArray();
        if (pdfTocs.Length == 0)
            return;

        AnsiConsole.Status().Start("Installing Chromium...", _ => Program.Main(new[] { "install", "chromium" }));
        AnsiConsole.MarkupLine("[green]Chromium installed.[/]");

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        using var app = builder.Build();
        app.UseServe(outputFolder);
        await app.StartAsync();
        var baseUrl = new Uri(app.Urls.First());

        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();

        foreach (var (url, toc) in pdfTocs)
        {
            var outputPath = Path.Combine(outputFolder, Path.GetDirectoryName(url) ?? "", toc.pdfFileName ?? Path.ChangeExtension(Path.GetFileName(url), ".pdf"));

            await CreatePdf(browser, new(baseUrl, url), toc, outputPath);
        }

        IEnumerable<(string, Outline)> GetPdfTocs()
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
    }

    static async Task CreatePdf(IBrowser browser, Uri outlineUrl, Outline outline, string outputPath)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), ".docfx", "pdf", "pages");
        Directory.CreateDirectory(tempDirectory);

        var pages = GetPages(outline).ToArray();
        if (pages.Length == 0)
            return;

        var pagesByNode = pages.ToDictionary(p => p.node);
        var pagesByUrl = new Dictionary<Uri, List<(Outline node, NamedDestinations namedDests)>>();
        var pageNumbers = new Dictionary<Outline, int>();
        var nextPageNumbers = new Dictionary<Outline, int>();
        var nextPageNumber = 1;
        var margin = outline.pdfMargin ?? "0.4in";

        await AnsiConsole.Progress().Columns(new SpinnerColumn(), new TaskDescriptionColumn { Alignment = Justify.Left }).StartAsync(async c =>
        {
            await Parallel.ForEachAsync(pages, async (item, CancellationToken) =>
            {
                var task = c.AddTask(item.url.PathAndQuery);
                var page = await browser.NewPageAsync();
                await page.GotoAsync(item.url.ToString());
                await page.AddScriptTagAsync(new() { Content = EnsureHeadingAnchorScript });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                var bytes = await page.PdfAsync(new()
                {
                    PrintBackground = outline.pdfPrintBackground,
                    Margin = new() { Bottom = margin, Top = margin, Left = margin, Right = margin },
                });
                File.WriteAllBytes(item.path, bytes);
                task.Value = task.MaxValue;
            });
        });

        AnsiConsole.Status().Start("Creating PDF...", _ => MergePdf());
        AnsiConsole.MarkupLine($"[green]PDF saved to {outputPath}[/]");

        IEnumerable<(string path, Uri url, Outline node)> GetPages(Outline outline)
        {
            if (!string.IsNullOrEmpty(outline.href))
            {
                var url = new Uri(outlineUrl, outline.href);
                if (url.Host == outlineUrl.Host)
                {
                    var id = Convert.ToHexString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(url.ToString()))).Substring(0, 6).ToLower();
                    var name = Regex.Replace(url.PathAndQuery, "\\W", "-").Trim('-');
                    yield return (Path.Combine(tempDirectory, $"{name}-{id}.pdf"), url, outline);
                }
            }

            if (outline.items != null)
                foreach (var item in outline.items)
                    foreach (var url in GetPages(item))
                        yield return url;
        }

        void MergePdf()
        {
            using var output = File.Create(outputPath);
            using var builder = new PdfDocumentBuilder(output);

            builder.DocumentInformation = new()
            {
                Producer = $"docfx ({typeof(PdfBuilder).Assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version})",
            };

            // Calculate page number
            var pageNumber = 1;
            foreach (var (path, url, node) in pages)
            {
                using var document = PdfDocument.Open(path);

                var key = CleanUrl(url);
                if (!pagesByUrl.TryGetValue(key, out var dests))
                    pagesByUrl[key] = dests = new();
                dests.Add((node, document.Structure.Catalog.NamedDestinations));

                pageNumbers[node] = pageNumber;
                pageNumber += document.NumberOfPages;
                nextPageNumbers[node] = pageNumber;
            }

            // Copy pages
            foreach (var (path, url, node) in pages)
            {
                var basePageNumber = pageNumbers[node] - 1;
                using var document = PdfDocument.Open(path);
                for (var i = 1; i <= document.NumberOfPages; i++)
                    builder.AddPage(document, i, a => CopyLink(a, basePageNumber));
            }

            builder.Bookmarks = new(CreateBookmarks(outline.items).ToArray());
        }

        PdfAction CopyLink(PdfAction action, int basePageNumber)
        {
            return action switch
            {
                GoToAction link => new GoToAction(new(basePageNumber + link.Destination.PageNumber, link.Destination.Type, link.Destination.Coordinates)),
                UriAction url => HandleUriAction(url),
                _ => action,
            };

            PdfAction HandleUriAction(UriAction url)
            {
                if (!Uri.TryCreate(url.Uri, UriKind.Absolute, out var uri) || !pagesByUrl.TryGetValue(CleanUrl(uri), out var pages))
                    return url;

                if (!string.IsNullOrEmpty(uri.Fragment) && uri.Fragment.Length > 1)
                {
                    var name = uri.Fragment.Substring(1);
                    foreach (var (node, namedDests) in pages)
                    {
                        if (namedDests.TryGet(name, out var dest) && dest is not null)
                        {
                            return new GoToAction(new(pageNumbers[node] + dest.PageNumber - 1, dest.Type, dest.Coordinates));
                        }
                    }

                    AnsiConsole.MarkupLine($"[yellow]Failed to resolve named dest: {name}[/]");
                }

                return new GoToAction(new(pageNumbers[pages[0].node], ExplicitDestinationType.XyzCoordinates, ExplicitDestinationCoordinates.Empty));
            }
        }

        static Uri CleanUrl(Uri url)
        {
            return new UriBuilder(url) { Query = null, Fragment = null }.Uri;
        }

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
                        new(nextPageNumber, ExplicitDestinationType.XyzCoordinates, ExplicitDestinationCoordinates.Empty),
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

                nextPageNumber = nextPageNumbers[item];
                yield return new DocumentBookmarkNode(
                    item.name, level,
                    new(pageNumbers[item], ExplicitDestinationType.XyzCoordinates, ExplicitDestinationCoordinates.Empty),
                    CreateBookmarks(item.items, level + 1).ToArray());
            }
        }
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
}
