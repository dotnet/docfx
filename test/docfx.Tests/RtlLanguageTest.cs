// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Docfx.Tests.Common;

using Microsoft.Playwright;

namespace Docfx.Tests;

/// <summary>
/// End-to-end tests for right-to-left languages in the modern template. Builds sites in
/// Persian, serves them over HTTP and drives a headless Chromium: the direction of the
/// page and of its code, the layout of API member names, and search in right-to-left languages.
/// </summary>
[Collection("docfx STA")]
public class RtlLanguageTest : TestBase
{
    static RtlLanguageTest()
    {
        PlaywrightHelper.EnsurePlaywrightNodeJsPath();
        Microsoft.Playwright.Program.Main(["install", "chromium", "--only-shell"]);
    }

    [Fact]
    public async Task RightToLeftPagesAreRenderedRightToLeft()
    {
        var directory = GetRandomFolder();

        // The same content in Persian is built twice: as a Persian site and as an English site.
        // The Persian site also translates the strings of the template.
        CreateFile("docfx-fa.json", Config("_site-fa", """[ "default", "modern", "tpl" ]""", """{ "_lang": "fa", "_enableSearch": true }"""), directory);
        CreateFile("docfx-en.json", Config("_site-en", """[ "default", "modern" ]""", """{ "_lang": "en", "_enableSearch": true }"""), directory);
        CreateFile("tpl/token.json",
            """
            {
              "inThisArticle": "در این مقاله",
              "tocTitle": "فهرست مطالب",
              "showToc": "نمایش فهرست مطالب",
              "toggleNavigation": "نمایش یا پنهان کردن منو",
              "close": "بستن",
              "anchor": "پیوند ثابت",
              "viewSource": "مشاهده کد",
              "search": "جستجو",
              "pagePrev": "صفحه قبل",
              "pageNext": "صفحه بعد"
            }
            """, directory);

        CreateFile("toc.yml",
            """
            - name: مستندات
              href: index.md
            - name: صفحه دیگر
              href: دیگر.md
            - name: مرجع
              href: api/
            """, directory);
        // The API pages have a table of contents of their own, shown at the side of the pages
        CreateFile("api/toc.yml",
            """
            - name: Calculator
              href: Demo.Calculator.yml
            """, directory);
        CreateFile("index.md",
            """
            ---
            title: مستندات نمونه
            description: یک سایت مستندات فارسی
            ---

            # مستندات نمونه

            این یک پاراگراف فارسی با کد `Console.WriteLine("سلام")` است.

            ## شروع به کار

            برای نصب دستور زیر را اجرا کنید:

            ```json
            {
              "key": "value"
            }
            ```

            > این یک نقل‌قول است.

            ## نصب

            [صفحه دیگر](دیگر.md)
            """, directory);
        // The name of this file is Persian too
        CreateFile("دیگر.md", "# صفحه دیگر\n\nمتن صفحه دیگر.", directory);
        CreateFile("api/Demo.Calculator.yml",
            """
            ### YamlMime:ManagedReference
            items:
            - uid: Demo.Calculator
              commentId: T:Demo.Calculator
              id: Calculator
              parent: Demo
              children:
              - Demo.Calculator.#ctor
              langs:
              - csharp
              name: Calculator
              nameWithType: Calculator
              fullName: Demo.Calculator
              type: Class
              source:
                href: https://github.com/example/demo/blob/main/Calculator.cs
              assemblies:
              - Demo
              namespace: Demo
              summary: ماشین‌حساب.
              syntax:
                content: public class Calculator
              inheritance:
              - System.Object
              inheritedMembers:
              - System.Object.GetType
            - uid: Demo.Calculator.#ctor
              commentId: M:Demo.Calculator.#ctor
              id: '#ctor'
              parent: Demo.Calculator
              langs:
              - csharp
              name: Calculator()
              nameWithType: Calculator.Calculator()
              fullName: Demo.Calculator.Calculator()
              type: Constructor
              source:
                href: https://github.com/example/demo/blob/main/Calculator.cs#L5
              assemblies:
              - Demo
              namespace: Demo
              syntax:
                content: public Calculator()
            references:
            - uid: System.Object
              commentId: T:System.Object
              isExternal: true
              href: https://learn.microsoft.com/dotnet/api/system.object
              name: object
              nameWithType: object
              fullName: object
            - uid: System.Object.GetType
              commentId: M:System.Object.GetType
              parent: System.Object
              isExternal: true
              href: https://learn.microsoft.com/dotnet/api/system.object.gettype
              name: object.GetType()
              nameWithType: object.GetType()
              fullName: object.GetType()
            """, directory);

        await Docset.Build(Path.Combine(directory, "docfx-fa.json"));
        await Docset.Build(Path.Combine(directory, "docfx-en.json"));

        await BrowserTestServer.RunInBrowserAsync(
            [
                ("/fa/", Path.Combine(directory, "_site-fa")),
                ("/en/", Path.Combine(directory, "_site-en")),
            ],
            async (page, baseUrl) =>
            {
                // ---- Right-to-left ----
                await page.GotoAsync($"{baseUrl}/fa/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");

                // The direction comes from the language, the page loads the right-to-left styles
                Assert.Equal("fa", await page.EvaluateAsync<string>("document.documentElement.lang"));
                Assert.Equal("rtl", await page.EvaluateAsync<string>("document.documentElement.dir"));
                Assert.True(await HasStylesheetAsync(page, "docfx.rtl.min.css"), "The page loads the right-to-left stylesheet");
                Assert.False(await HasStylesheetAsync(page, "docfx.min.css"), "The page does not load the left-to-right stylesheet");

                // Code stays left-to-right: the braces of the JSON sample keep their place
                Assert.Equal("ltr", await StyleOfAsync(page, "article pre", "direction"));
                Assert.Equal("left", await StyleOfAsync(page, "article pre", "textAlign"));
                Assert.True(await page.EvaluateAsync<bool>("""
                    () => {
                      const pre = document.querySelector('article pre')
                      const walker = document.createTreeWalker(pre, NodeFilter.SHOW_TEXT)
                      const nodes = []
                      while (walker.nextNode()) { nodes.push(walker.currentNode) }
                      const rectOf = char => {
                        const node = nodes.find(n => n.data.includes(char))
                        const range = document.createRange()
                        range.setStart(node, node.data.indexOf(char))
                        range.setEnd(node, node.data.indexOf(char) + 1)
                        return range.getBoundingClientRect()
                      }
                      const open = rectOf('{'), close = rectOf('}')
                      // The code element has the padding of the block
                      const code = pre.querySelector('code')
                      const left = code.getBoundingClientRect().left + parseFloat(getComputedStyle(code).paddingLeft)
                      return open.top < close.top && Math.abs(open.left - left) < 1 && Math.abs(close.left - left) < 1
                    }
                    """));

                // The icon of the search box is at the start of its input, next to the text
                Assert.True(await IconIsAtStartOfInputAsync(page, "nav.navbar form.search", startOnTheRight: true), "The icon of the search box is on the right of its input");

                // The letters of the cursive Arabic script are joined
                Assert.Equal("در این مقاله", (await page.Locator("#affix h5").InnerTextAsync()).Trim());
                Assert.Equal("normal", await StyleOfAsync(page, "#affix h5", "letterSpacing"));

                // The bar of a blockquote is at the start of the text
                Assert.Equal("3px", await StyleOfAsync(page, "article blockquote", "borderRightWidth"));
                Assert.Equal("0px", await StyleOfAsync(page, "article blockquote", "borderLeftWidth"));

                // The strings of the template are translated
                Assert.Equal("نمایش یا پنهان کردن منو", await page.Locator("button[aria-controls=navpanel]").GetAttributeAsync("aria-label"));
                Assert.Equal("جستجو", await page.Locator("#search-query").GetAttributeAsync("aria-label"));
                Assert.Equal("پیوند ثابت", await page.Locator("article h2 a.anchorjs-link").First.GetAttributeAsync("aria-label"));

                // A page whose file name is Persian is served like any other page. Previous and
                // next point to where the articles are: reading continues on the left.
                await page.GotoAsync($"{baseUrl}/fa/{Uri.EscapeDataString("دیگر.html")}");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                Assert.Equal("صفحه دیگر", (await page.Locator("article h1").InnerTextAsync()).Trim());
                Assert.Contains("bi-chevron-right", await page.Locator("#nextArticle .prev i").GetAttributeAsync("class"));
                Assert.Contains("bi-chevron-left", await page.Locator("#nextArticle .next i").GetAttributeAsync("class"));
                Assert.True(await page.EvaluateAsync<bool>("""
                    () => {
                      const next = document.querySelector('#nextArticle .next').getBoundingClientRect()
                      const prev = document.querySelector('#nextArticle .prev').getBoundingClientRect()
                      return next.right <= prev.left
                    }
                    """));

                // API pages: member names are laid out left to right, so that they keep their
                // parentheses, and they are aligned to the start of the line, on the right
                await page.GotoAsync($"{baseUrl}/fa/api/Demo.Calculator.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                var layout = await page.EvaluateAsync<bool[]>("""
                    () => {
                      const laidOutLeftToRight = element => {
                        // The name is laid out by its first character in an element with dir="auto"
                        const node = (element.querySelector('[dir=auto]') ?? element).firstChild
                        const text = node.textContent
                        const left = index => {
                          const range = document.createRange()
                          range.setStart(node, index)
                          range.setEnd(node, index + 1)
                          return range.getBoundingClientRect().left
                        }
                        return left(text.search(/\S/)) < left(text.search(/\S\s*$/))
                      }
                      const heading = document.querySelector('h3[data-uid]')
                      const inherited = [...document.querySelectorAll('a.xref')].find(a => a.textContent.trim() === 'object.GetType()')
                      const name = heading.querySelector('[dir=auto]').getBoundingClientRect()
                      const [base, derived] = document.querySelectorAll('dl.typelist.inheritance dd > div')

                      return [
                        heading.textContent.trim() === 'Calculator()',
                        laidOutLeftToRight(heading),
                        Boolean(inherited) && laidOutLeftToRight(inherited),
                        Math.abs(name.right - (heading.getBoundingClientRect().right - parseFloat(getComputedStyle(heading).paddingRight))) < 1,
                        // The inheritance chain starts at the base type, on the right like all text
                        base.getBoundingClientRect().left >= derived.getBoundingClientRect().right
                      ]
                    }
                    """);
                Assert.True(layout[0], "The constructor is rendered with its name");
                Assert.True(layout[1], "The name of the constructor is laid out left to right");
                Assert.True(layout[2], "Inherited members are laid out left to right");
                Assert.True(layout[3], "The name is aligned to the start of the line");
                Assert.True(layout[4], "The inheritance chain starts at the base type");
                Assert.Equal("مشاهده کد", await page.Locator("h1[data-uid] .header-action").GetAttributeAsync("title"));

                // The icon of the TOC filter is at the start of its input too, and the table of
                // contents is at the start of the line, on the right
                Assert.True(await IconIsAtStartOfInputAsync(page, ".toc form.filter", startOnTheRight: true), "The icon of the TOC filter is on the right of its input");
                Assert.True(await page.EvaluateAsync<bool>("""
                    () => {
                      const toc = document.querySelector('#toc').getBoundingClientRect()
                      const article = document.querySelector('article').getBoundingClientRect()
                      return toc.left >= article.right
                    }
                    """));

                // The strings of the table of contents are translated too
                Assert.Equal("فهرست مطالب", await page.Locator("#tocOffcanvasLabel").InnerTextAsync());
                Assert.Equal("بستن", await page.Locator("#tocOffcanvas .btn-close").GetAttributeAsync("aria-label"));
                Assert.Equal("نمایش فهرست مطالب", await page.Locator("button[aria-controls=tocOffcanvas]").GetAttributeAsync("aria-label"));

                // ---- Left-to-right ----
                await page.GotoAsync($"{baseUrl}/en/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");

                // Pages in left-to-right languages keep the default direction and styles
                Assert.Equal("en", await page.EvaluateAsync<string>("document.documentElement.lang"));
                Assert.Null(await page.EvaluateAsync<string>("document.documentElement.getAttribute('dir')"));
                Assert.True(await HasStylesheetAsync(page, "docfx.min.css"), "The page loads the left-to-right stylesheet");
                Assert.False(await HasStylesheetAsync(page, "docfx.rtl.min.css"), "The page does not load the right-to-left stylesheet");

                // The left-to-right styles are not changed: text is aligned to the start of the line
                Assert.Equal("ltr", await StyleOfAsync(page, "article pre", "direction"));
                Assert.Equal("start", await StyleOfAsync(page, "article pre", "textAlign"));
                Assert.True(await IconIsAtStartOfInputAsync(page, "nav.navbar form.search", startOnTheRight: false), "The icon of the search box is on the left of its input");
                // innerText returns the rendered text: the title is upper-cased, and its letters are set apart
                Assert.Equal("IN THIS ARTICLE", (await page.Locator("#affix h5").InnerTextAsync()).Trim());
                Assert.Equal("2px", await StyleOfAsync(page, "#affix h5", "letterSpacing"));
                Assert.Equal("0px", await StyleOfAsync(page, "article blockquote", "borderRightWidth"));
                Assert.Equal("3px", await StyleOfAsync(page, "article blockquote", "borderLeftWidth"));

                Assert.Equal("Toggle navigation", await page.Locator("button[aria-controls=navpanel]").GetAttributeAsync("aria-label"));
                Assert.Equal("Search", await page.Locator("#search-query").GetAttributeAsync("aria-label"));
                Assert.Equal("Anchor", await page.Locator("article h2 a.anchorjs-link").First.GetAttributeAsync("aria-label"));

                await page.GotoAsync($"{baseUrl}/en/{Uri.EscapeDataString("دیگر.html")}");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                Assert.Contains("bi-chevron-left", await page.Locator("#nextArticle .prev i").GetAttributeAsync("class"));
                Assert.Contains("bi-chevron-right", await page.Locator("#nextArticle .next i").GetAttributeAsync("class"));

                // The table of contents is at the left
                await page.GotoAsync($"{baseUrl}/en/api/Demo.Calculator.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                Assert.True(await IconIsAtStartOfInputAsync(page, ".toc form.filter", startOnTheRight: false), "The icon of the TOC filter is on the left of its input");
                Assert.True(await page.EvaluateAsync<bool>("""
                    () => {
                      const toc = document.querySelector('#toc').getBoundingClientRect()
                      const article = document.querySelector('article').getBoundingClientRect()
                      return toc.right <= article.left
                    }
                    """));
                Assert.Equal("Table of Contents", await page.Locator("#tocOffcanvasLabel").InnerTextAsync());
                Assert.Equal("Close", await page.Locator("#tocOffcanvas .btn-close").GetAttributeAsync("aria-label"));
                Assert.Equal("Show table of contents", await page.Locator("button[aria-controls=tocOffcanvas]").GetAttributeAsync("aria-label"));

                // The inheritance chain starts at the base type, on the left
                Assert.True(await page.EvaluateAsync<bool>("""
                    () => {
                      const [base, derived] = document.querySelectorAll('dl.typelist.inheritance dd > div')
                      return base.getBoundingClientRect().right <= derived.getBoundingClientRect().left
                    }
                    """), "The inheritance chain starts at the base type");
            });
    }

    [Fact]
    public async Task SearchFindsWordsOfRightToLeftLanguages()
    {
        var directory = GetRandomFolder();

        // The same documents in Persian, Arabic and Hebrew, searched with different lunr languages
        CreateFile("toc.yml",
            """
            - name: شروع
              href: fa.md
            - name: الکتاب
              href: ar.md
            - name: ספרים
              href: he.md
            """, directory);
        CreateFile("fa.md", "---\nuid: fa\n---\n\n# شروع به کار\n\nبرای نصب docfx دستور زیر را اجرا کنید. برنامه‌نویسی", directory);
        CreateFile("ar.md", "---\nuid: ar\n---\n\n# الكتاب\n\nالكتاب المدرسي", directory);
        CreateFile("he.md", "---\nuid: he\n---\n\n# ספרים\n\nהספר והספרים בספרייה", directory);

        // The sites differ in the lunr languages of their main.js only
        (string name, string? template, string? languages)[] sites =
        [
            ("default", null, null),
            ("fa-lang", "tpl-fa", "['fa']"),
            ("ar", "tpl-ar", "['en', 'ar']"),
            ("all", "tpl-all", "['en', 'ar', 'he']"),
        ];
        foreach (var (name, template, languages) in sites)
        {
            CreateFile($"docfx-{name}.json", Config($"_site-{name}", $$"""[ "default", "modern"{{(template is null ? "" : $", \"{template}\"")}} ]""", """{ "_enableSearch": true }"""), directory);
            if (template is not null)
            {
                CreateFile($"{template}/public/main.js", $"export default {{ lunrLanguages: {languages} }}", directory);
            }
            await Docset.Build(Path.Combine(directory, $"docfx-{name}.json"));
        }

        await BrowserTestServer.RunInBrowserAsync(
            [.. sites.Select(site => ($"/{site.name}/", Path.Combine(directory, $"_site-{site.name}")))],
            async (page, baseUrl) =>
            {
                var console = new List<string>();
                page.Console += (_, message) => console.Add(message.Text);

                // Without lunr languages, lunr's trimmer used to trim Persian words to nothing
                Assert.Equal(new[] { "شروع به کار" }, await SearchAsync(page, baseUrl, "/default/", "شروع"));
                Assert.Equal(new[] { "شروع به کار" }, await SearchAsync(page, baseUrl, "/default/", "نصب"));
                // The parts of a word that are joined by a zero-width non-joiner are found
                Assert.Equal(new[] { "شروع به کار" }, await SearchAsync(page, baseUrl, "/default/", "نویسی"));
                Assert.Equal(new[] { "شروع به کار" }, await SearchAsync(page, baseUrl, "/default/", "docfx"));

                // An unsupported language is reported and doesn't disable search
                Assert.Equal(new[] { "شروع به کار" }, await SearchAsync(page, baseUrl, "/fa-lang/", "شروع"));
                Assert.Contains(console, message => message.Contains("Search does not support the language \"fa\""));

                // The Arabic language pack stems the words: "book" finds "the book"
                Assert.Equal(new[] { "الكتاب" }, await SearchAsync(page, baseUrl, "/ar/", "كتاب"));
                Assert.Equal(new[] { "شروع به کار" }, await SearchAsync(page, baseUrl, "/ar/", "docfx"));

                // The Arabic pack keeps working once the Hebrew pack is loaded too
                Assert.Equal(new[] { "الكتاب" }, await SearchAsync(page, baseUrl, "/all/", "كتاب"));
                Assert.Equal(new[] { "ספרים" }, await SearchAsync(page, baseUrl, "/all/", "ספרים"));
                Assert.Equal(new[] { "شروع به کار" }, await SearchAsync(page, baseUrl, "/all/", "docfx"));

                // Loading the language packs raised no error
                Assert.DoesNotContain(console, message => message.Contains("Cannot load the search language"));
                Assert.DoesNotContain(console, message => message.Contains("TypeError"));
            });
    }

    private static string Config(string dest, string templates, string globalMetadata)
    {
        return $$"""
            {
              "build": {
                "content": [{ "files": [ "**/*.md", "**/*.yml" ], "exclude": [ "_site*/**" ] }],
                "template": {{templates}},
                "dest": "{{dest}}",
                "globalMetadata": {{globalMetadata}}
              }
            }
            """;
    }

    private static async Task<IReadOnlyList<string>> SearchAsync(IPage page, string baseUrl, string site, string query)
    {
        await page.GotoAsync($"{baseUrl}{site}fa.html");
        // The search box is enabled once the search index is ready
        await page.WaitForFunctionAsync("!document.getElementById('search-query').disabled");
        await page.Locator("#search-query").FillAsync(query);
        await page.Locator("#search-results .sr-item, #search-results .search-list").First.WaitForAsync();
        return await page.Locator("#search-results .sr-item .item-title").AllInnerTextsAsync();
    }

    private static Task<bool> HasStylesheetAsync(IPage page, string file)
    {
        return page.EvaluateAsync<bool>($"[...document.querySelectorAll('link[rel=stylesheet]')].some(link => link.getAttribute('href').endsWith('{file}'))");
    }

    private static Task<string> StyleOfAsync(IPage page, string selector, string property)
    {
        return page.Locator(selector).First.EvaluateAsync<string>($"(element, property) => getComputedStyle(element)[property]", property);
    }

    private static Task<bool> IconIsAtStartOfInputAsync(IPage page, string form, bool startOnTheRight)
    {
        return page.EvaluateAsync<bool>("""
            ({ form, startOnTheRight }) => {
              const icon = document.querySelector(`${form} i.bi`).getBoundingClientRect()
              const input = document.querySelector(`${form} input`).getBoundingClientRect()
              const middle = input.left + input.width / 2
              return startOnTheRight ? icon.left > middle : icon.right < middle
            }
            """, new { form, startOnTheRight });
    }
}
