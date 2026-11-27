// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Docfx.Common;
using Docfx.Tests.Common;

namespace Docfx.Tests;

/// <summary>
/// Tests the text direction that docfx writes to the pages of a docset.
/// </summary>
[Collection("docfx STA")]
public class TextDirectionBuildTest : TestBase
{
    private const string ModernTemplate = """[ "default", "modern" ]""";

    [Fact]
    public async Task PagesGetTheDirectionOfTheirLanguage()
    {
        var directory = GetRandomFolder();
        CreateFile("index.md", "# Home\n\n## Section\n\nText.", directory);

        // (name, global metadata, templates, html tag, stylesheet of the modern template)
        (string Name, string GlobalMetadata, string Templates, string HtmlTag, string? Stylesheet)[] cases =
        [
            // Right-to-left languages get dir="rtl" and the right-to-left stylesheet
            ("fa", """{ "_lang": "fa" }""", ModernTemplate, """<html lang="fa" dir="rtl">""", "docfx.rtl.min.css"),
            ("ar", """{ "_lang": "ar-SA" }""", ModernTemplate, """<html lang="ar-SA" dir="rtl">""", "docfx.rtl.min.css"),
            ("he", """{ "_lang": "he" }""", ModernTemplate, """<html lang="he" dir="rtl">""", "docfx.rtl.min.css"),
            ("ur", """{ "_lang": "ur-PK" }""", ModernTemplate, """<html lang="ur-PK" dir="rtl">""", "docfx.rtl.min.css"),
            ("pa-arab", """{ "_lang": "pa-Arab" }""", ModernTemplate, """<html lang="pa-Arab" dir="rtl">""", "docfx.rtl.min.css"),

            // Left-to-right languages keep the browser default
            ("en", """{ "_lang": "en" }""", ModernTemplate, """<html lang="en">""", "docfx.min.css"),
            ("ja", """{ "_lang": "ja-JP" }""", ModernTemplate, """<html lang="ja-JP">""", "docfx.min.css"),
            ("pa", """{ "_lang": "pa" }""", ModernTemplate, """<html lang="pa">""", "docfx.min.css"),
            ("no-language", "{}", ModernTemplate, "<html>", "docfx.min.css"),

            // The direction can be set explicitly, e.g. for a language docfx doesn't know
            ("dir-rtl", """{ "_dir": "rtl" }""", ModernTemplate, """<html dir="rtl">""", "docfx.rtl.min.css"),
            ("dir-ltr", """{ "_lang": "fa", "_dir": "ltr" }""", ModernTemplate, """<html lang="fa" dir="ltr">""", "docfx.min.css"),

            // The default template writes the direction too
            ("default-template", """{ "_lang": "fa" }""", """[ "default" ]""", """<html lang="fa" dir="rtl">""", null),
        ];

        foreach (var testCase in cases)
        {
            var site = await BuildAsync(directory, testCase.Name, testCase.GlobalMetadata, testCase.Templates);
            var html = File.ReadAllText(Path.Combine(site, "index.html"));

            Assert.Contains(testCase.HtmlTag, html);
            if (testCase.Stylesheet is not null)
            {
                // The page loads the stylesheet of its direction only
                var stylesheet = Assert.Single(html.Split('\n'), line => line.Contains("stylesheet") && line.Contains("docfx."));
                Assert.Equal($"""<link rel="stylesheet" href="public/{testCase.Stylesheet}">""", stylesheet.Trim());
                Assert.True(File.Exists(Path.Combine(site, "public", testCase.Stylesheet)), $"{testCase.Stylesheet} is copied to the site");
            }
        }
    }

    [Fact]
    public async Task APagesDirectionCanBeSetPerFile()
    {
        var directory = GetRandomFolder();

        // An English docset with an Arabic article
        CreateFile("index.md", "# Home\n\nText.", directory);
        CreateFile("arabic.md", "# مقدمة\n\nنص.", directory);
        var site = await BuildAsync(directory, "lang", """{ "_lang": "en" }""", ModernTemplate,
            fileMetadata: """{ "_lang": { "arabic.md": "ar" } }""");
        Assert.Contains("""<html lang="en">""", File.ReadAllText(Path.Combine(site, "index.html")));
        Assert.Contains("""<html lang="ar" dir="rtl">""", File.ReadAllText(Path.Combine(site, "arabic.html")));

        // A Persian docset with a page that is left-to-right
        CreateFile("index.md", "# Home\n\nText.", directory);
        CreateFile("ltr.md", "# Left to right\n\nText.", directory);
        site = await BuildAsync(directory, "dir", """{ "_lang": "fa" }""", ModernTemplate,
            fileMetadata: """{ "_dir": { "ltr.md": "ltr" } }""");
        Assert.Contains("""<html lang="fa" dir="rtl">""", File.ReadAllText(Path.Combine(site, "index.html")));
        Assert.Contains("""<html lang="fa" dir="ltr">""", File.ReadAllText(Path.Combine(site, "ltr.html")));
    }

    [Fact]
    public async Task AnInvalidDirectionIsReportedOnce()
    {
        var directory = GetRandomFolder();
        CreateFile("index.md", "# Home\n\nText.", directory);
        CreateFile("toc.yml", "- name: Home\n  href: index.md", directory);

        // The warning is logged once for the docset, not for every page. The direction falls
        // back to the language.
        var listener = TestLoggerListener.CreateLoggerListenerWithCodeFilter(WarningCodes.Build.InvalidTextDirection);
        Logger.RegisterListener(listener);
        string html;
        try
        {
            var site = await BuildAsync(directory, "invalid", """{ "_lang": "fa", "_dir": "auto" }""", ModernTemplate);
            html = File.ReadAllText(Path.Combine(site, "index.html"));
        }
        finally
        {
            Logger.UnregisterListener(listener);
        }

        var warning = Assert.Single(listener.Items);
        Assert.Equal("""Invalid value "auto" for _dir, expected "rtl" or "ltr".""", warning.Message);
        Assert.Contains("""<html lang="fa" dir="rtl">""", html);
    }

    private static async Task<string> BuildAsync(string directory, string name, string globalMetadata, string templates, string? fileMetadata = null)
    {
        var config = CreateFile($"docfx-{name}.json",
            $$"""
            {
              "build": {
                "content": [{ "files": [ "**/*.md", "**/*.yml" ], "exclude": [ "_site*/**" ] }],
                "template": {{templates}},
                "dest": "_site-{{name}}",
                "globalMetadata": {{globalMetadata}}{{(fileMetadata is null ? "" : $",\n    \"fileMetadata\": {fileMetadata}")}}
              }
            }
            """, directory);
        await Docset.Build(config);
        return Path.Combine(directory, $"_site-{name}");
    }
}
