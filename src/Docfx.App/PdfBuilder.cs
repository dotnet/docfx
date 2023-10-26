﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Playwright;
using Spectre.Console;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Actions;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Outline.Destinations;
using UglyToad.PdfPig.Writer;

namespace Docfx.Pdf;

static class PdfBuilder
{
    class Outline
    {
        public string name { get; init; } = "";

        public string? href { get; init; }

        public Outline[]? items { get; init; }
    }

    public static async Task CreatePdf(Uri outlineUrl, CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient();

        var outline = await AnsiConsole.Status().StartAsync(
            $"Downloading {outlineUrl}...",
            c => http.GetFromJsonAsync<Outline>(outlineUrl, cancellationToken));

        if (outline is null)
            return;

        AnsiConsole.Status().Start(
            "Installing Chromium...",
            c => Program.Main(new[] { "install", "chromium" }));

        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();

        var tempDirectory = Path.Combine(Path.GetTempPath(), ".docfx", "pdf", "pages");
        Directory.CreateDirectory(tempDirectory);

        var pages = GetPages(outline).ToArray();
        if (pages.Length == 0)
        {
            // TODO: Warn
            return;
        }

        var pagesByNode = pages.ToDictionary(p => p.node);
        var pagesByUrl = new Dictionary<Uri, List<(Outline node, NamedDestinations namedDests)>>();
        var pageNumbers = new Dictionary<Outline, int>();
        var nextPageNumbers = new Dictionary<Outline, int>();
        var nextPageNumber = 1;
        var margin = "0.4in";

        await AnsiConsole.Progress().Columns(new SpinnerColumn(), new TaskDescriptionColumn { Alignment = Justify.Left }).StartAsync(async c =>
        {
            await Parallel.ForEachAsync(pages, async (item, CancellationToken) =>
            {
                var task = c.AddTask(item.url.ToString());
                var page = await browser.NewPageAsync();
                await page.GotoAsync(item.url.ToString());
                var bytes = await page.PdfAsync(new() { Margin = new() { Bottom = margin, Top = margin, Left = margin, Right = margin } });
                File.WriteAllBytes(item.path, bytes);
                task.Value = task.MaxValue;
            });
        });

        AnsiConsole.Status().Start("Creating PDF...", _ => MergePdf());

        IEnumerable<(string path, Uri url, Outline node)> GetPages(Outline outline)
        {
            if (!string.IsNullOrEmpty(outline.href))
            {
                var url = new Uri(outlineUrl, outline.href);
                if (url.Host == outlineUrl.Host)
                {
                    var id = Convert.ToHexString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(url.ToString())));
                    yield return (Path.Combine(tempDirectory, $"{id}.pdf"), url, outline);
                }
            }

            if (outline.items != null)
                foreach (var item in outline.items)
                    foreach (var url in GetPages(item))
                        yield return url;
        }

        void MergePdf()
        {
            using var output = File.Create("output.pdf");
            using var builder = new PdfDocumentBuilder(output);

            builder.DocumentInformation = new()
            {
                Title = "",
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
                using var document = PdfDocument.Open(path);
                for (var i = 1; i <= document.NumberOfPages; i++)
                    builder.AddPage(document, i, CopyLink);
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
                if (!Uri.TryCreate(url.Uri, UriKind.Absolute, out var uri) || !pagesByUrl.TryGetValue(CleanUrl(uri), out var pages))
                    return url;

                if (!string.IsNullOrEmpty(uri.Fragment) && uri.Fragment.Length > 1)
                {
                    var name = uri.Fragment.Substring(1);
                    foreach (var (node, namedDests) in pages)
                    {
                        if (namedDests.TryGet(name, out var dest))
                        {
                            AnsiConsole.MarkupLine($"[green]Resolve succeed: {name}[/]");
                            return new GoToAction(new(1, dest.Type, dest.Coordinates));
                        }
                    }

                    AnsiConsole.MarkupLine($"[yellow]Failed to resolve named dest: {name}[/]");
                }

                return new GoToAction(new(pageNumbers[pages[0].node], ExplicitDestinationType.FitHorizontally, ExplicitDestinationCoordinates.Empty));
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

                nextPageNumber = nextPageNumbers[item];
                yield return new DocumentBookmarkNode(
                    item.name, level,
                    new(pageNumbers[item], ExplicitDestinationType.FitHorizontally, ExplicitDestinationCoordinates.Empty),
                    CreateBookmarks(item.items, level + 1).ToArray());
            }
        }
    }
}
