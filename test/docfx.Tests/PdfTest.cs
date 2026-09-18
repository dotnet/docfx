// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Outline;

namespace Docfx.Tests;

[Collection("docfx STA")]
public class PdfTest : TestBase
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task HeaderFooterOnCoverAndToc(bool? onCover, bool? onToc)
    {
        using var pdf = await BuildPdf(onCover, onToc, customTemplates: true);

        AssertPages(pdf);
        foreach (var page in pdf.GetPages())
        {
            var showHeaderFooter = page.Number switch
            {
                1 => onCover is true,
                2 or 3 => onToc is true,
                _ => true,
            };

            var header = string.Concat(page.Letters.Where(l => l.GlyphRectangle.Bottom > page.Height - 50).Select(l => l.Value));
            var footer = string.Concat(page.Letters.Where(l => l.GlyphRectangle.Top < 50).Select(l => l.Value));
            if (showHeaderFooter)
            {
                Assert.Equal($"HEADER-{page.Number}-OF-5", header);
                Assert.Equal($"FOOTER-{page.Number}-OF-5", footer);
            }
            else
            {
                Assert.Empty(header);
                Assert.Empty(footer);
                Assert.DoesNotContain("HEADER-", page.Text);
                Assert.DoesNotContain("FOOTER-", page.Text);
            }
        }
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task DefaultFooterOnCoverAndToc(bool? onCover, bool? onToc)
    {
        using var pdf = await BuildPdf(onCover, onToc, customTemplates: false);

        AssertPages(pdf);
        foreach (var page in pdf.GetPages())
        {
            var showFooter = page.Number switch
            {
                1 => onCover is true,
                2 or 3 => onToc is true,
                _ => true,
            };

            var header = string.Concat(page.Letters.Where(l => l.GlyphRectangle.Bottom > page.Height - 50).Select(l => l.Value));
            var footer = string.Concat(page.Letters.Where(l => l.GlyphRectangle.Top < 50).Select(l => l.Value));
            Assert.Empty(header);
            Assert.Equal(showFooter ? $"{page.Number} / 5" : "", footer);
        }
    }

    private async Task<PdfDocument> BuildPdf(bool? onCover, bool? onToc, bool customTemplates)
    {
        var directory = GetRandomFolder();
        var configPath = CreateFile("docfx.json",
            """
            {
                "build": {
                    "content": [{ "files": [ "toc.yml" ] }],
                    "resource": [{ "files": [ "*.html", "public/*.css" ] }],
                    "template": ["default"],
                    "dest": "_site"
                }
            }
            """, directory);

        var toc =
            """
            pdf: true
            pdfCoverPage: cover.html
            pdfTocPage: true
            items:
            - name: ChapterOne
              href: first.html
            - name: ChapterTwo
              href: second.html
            """;
        if (onCover.HasValue)
            toc += $"\npdfHeaderFooterOnCover: {onCover.Value.ToString().ToLowerInvariant()}";
        if (onToc.HasValue)
            toc += $"\npdfHeaderFooterOnToc: {onToc.Value.ToString().ToLowerInvariant()}";
        if (customTemplates)
        {
            toc += """

                pdfHeaderTemplate: '<div style="font-size: 12px;">HEADER-<span class="pageNumber"></span>-OF-<span class="totalPages"></span></div>'
                pdfFooterTemplate: footer.html
                """;
            CreateFile("footer.html",
                """<div style="font-size: 12px;">FOOTER-<span class='pageNumber'></span>-OF-<span class='totalPages'></span></div>""", directory);
        }

        CreateFile("toc.yml", toc, directory);
        CreateFile("public/main.css",
            """
            @page { size: A4; margin: 1in; }
            body { font-family: Arial, sans-serif; font-size: 16px; }
            .pdftoc li + li { break-before: page; }
            """, directory);
        CreateFile("public/docfx.min.css", "", directory);
        CreateFile("cover.html", Html("Cover body"), directory);
        CreateFile("first.html", Html("First article body"), directory);
        CreateFile("second.html", Html("Second article body"), directory);

        await Docset.Build(configPath);
        await Docset.Pdf(configPath);

        return PdfDocument.Open(Path.Combine(directory, "_site", "toc.pdf"));

        static string Html(string body) =>
            $"""<!DOCTYPE html><html><head><link rel="stylesheet" href="public/main.css"></head><body>{body}</body></html>""";
    }

    private static void AssertPages(PdfDocument pdf)
    {
        Assert.Equal(5, pdf.NumberOfPages);
        Assert.Contains("Cover body", pdf.GetPage(1).Text);
        Assert.Contains("Table of Contents", pdf.GetPage(2).Text);
        Assert.Matches(@"ChapterOne\s*4", pdf.GetPage(2).Text);
        Assert.Matches(@"ChapterTwo\s*5", pdf.GetPage(3).Text);
        Assert.Contains("First article body", pdf.GetPage(4).Text);
        Assert.Contains("Second article body", pdf.GetPage(5).Text);

        Assert.True(pdf.TryGetBookmarks(out var bookmarks));
        Assert.Collection(bookmarks.Roots,
            bookmark => Assert.Equal(4, Assert.IsType<DocumentBookmarkNode>(bookmark).Destination.PageNumber),
            bookmark => Assert.Equal(5, Assert.IsType<DocumentBookmarkNode>(bookmark).Destination.PageNumber));
    }
}
