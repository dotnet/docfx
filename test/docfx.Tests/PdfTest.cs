// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
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

    [Theory]
    [InlineData("A4 landscape", 842, 595, true)]
    [InlineData("A3 portrait", 842, 1191, true)]
    [InlineData("160mm 230mm", 454, 652, true)]
    [InlineData("230mm 160mm", 652, 454, true)]
    [InlineData("A4 landscape", 842, 595, false)]
    public async Task StaticHeaderFooterOnDifferentPageSizes(string coverPageSize, double coverWidth, double coverHeight, bool onCover)
    {
        using var pdf = await BuildPdf(onCover, true, customTemplates: true, coverPageSize, staticTemplates: true);

        AssertPages(pdf);
        Assert.InRange(pdf.GetPage(1).Width, coverWidth - 1, coverWidth + 1);
        Assert.InRange(pdf.GetPage(1).Height, coverHeight - 1, coverHeight + 1);

        foreach (var page in pdf.GetPages())
        {
            if (page.Number > 1)
            {
                Assert.InRange(page.Width, 594, 596);
                Assert.InRange(page.Height, 841, 843);
            }

            var showHeaderFooter = page.Number > 1 || onCover;
            AssertMargin(page, showHeaderFooter ? "HEADER-STATIC" : "", header: true);
            AssertMargin(page, showHeaderFooter ? "FOOTER-STATIC" : "", header: false);
        }

        static void AssertMargin(Page page, string expected, bool header)
        {
            var letters = page.Letters.Where(l => header
                ? l.GlyphRectangle.Bottom > page.Height - 50
                : l.GlyphRectangle.Top < 50).ToArray();

            Assert.Equal(expected, string.Concat(letters.Select(l => l.Value)));
            if (expected.Length == 0)
                return;

            Assert.All(letters, letter =>
            {
                Assert.InRange(letter.GlyphRectangle.Left, 0, page.Width);
                Assert.InRange(letter.GlyphRectangle.Right, 0, page.Width);
                Assert.InRange(letter.GlyphRectangle.Bottom, header ? page.Height - 50 : 0, header ? page.Height : 50);
                Assert.InRange(letter.GlyphRectangle.Top, header ? page.Height - 50 : 0, header ? page.Height : 50);
            });
            Assert.InRange(letters.Max(l => l.GlyphRectangle.Right), page.Width - 50, page.Width);
        }
    }

    private async Task<PdfDocument> BuildPdf(
        bool? onCover, bool? onToc, bool customTemplates, string coverPageSize = null, bool staticTemplates = false)
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
            var header = staticTemplates
                ? "HEADER-STATIC"
                : """HEADER-<span class="pageNumber"></span>-OF-<span class="totalPages"></span>""";
            var footer = staticTemplates
                ? "FOOTER-STATIC"
                : """FOOTER-<span class='pageNumber'></span>-OF-<span class='totalPages'></span>""";
            toc += $"""

                pdfHeaderTemplate: '{Template(header)}'
                pdfFooterTemplate: footer.html
                """;
            CreateFile("footer.html", Template(footer), directory);
        }

        CreateFile("toc.yml", toc, directory);
        CreateFile("public/main.css",
            """
            @page { size: A4; margin: 1in; }
            body { font-family: Arial, sans-serif; font-size: 16px; }
            .pdftoc li + li { break-before: page; }
            """, directory);
        CreateFile("public/docfx.min.css", "", directory);
        CreateFile("cover.html", Html("Cover body", coverPageSize), directory);
        CreateFile("first.html", Html("First article body"), directory);
        CreateFile("second.html", Html("Second article body"), directory);

        await Docset.Build(configPath);
        await Docset.Pdf(configPath);

        return PdfDocument.Open(Path.Combine(directory, "_site", "toc.pdf"));

        static string Html(string body, string pageSize = null) =>
            $$"""<!DOCTYPE html><html><head><link rel="stylesheet" href="public/main.css"><style>@page { size: {{pageSize ?? "A4"}}; }</style></head><body>{{body}}</body></html>""";

        static string Template(string text) =>
            $"""<div style="width: 100%; font-size: 12px;"><div style="float: right; padding: 0 2em;">{text}</div></div>""";
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
