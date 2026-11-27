// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;

using Docfx.Tests.Common;

using Microsoft.Playwright;

namespace Docfx.Tests;

/// <summary>
/// End-to-end tests for the version selector and the locale selector of the modern template.
/// Builds multi-version, multi-locale and combined sites, serves them over HTTP and drives a
/// headless Chromium through the selectors' navigation behavior.
/// </summary>
[Collection("docfx STA")]
public class VersionLocaleSelectorTest : TestBase
{
    static VersionLocaleSelectorTest()
    {
        PlaywrightHelper.EnsurePlaywrightNodeJsPath();
        Microsoft.Playwright.Program.Main(["install", "chromium", "--only-shell"]);
    }

    [Fact]
    public async Task VersionSelectorNavigatesAndFallsBackToHomePage()
    {
        var directory = GetRandomFolder();

        // Build a versioned site: the default group (stable) at the site root,
        // and one group per version in a subfolder
        var configPath = CreateFile("docfx.json", """
            {
              "build": {
                "content": [
                  { "files": [ "**/*.md" ], "src": "stable", "dest": "." },
                  { "files": [ "**/*.md" ], "src": "main", "dest": ".", "group": "main" },
                  { "files": [ "**/*.md" ], "src": "v1.0", "dest": ".", "group": "v1.0" }
                ],
                "template": [ "default", "modern", "tpl-v" ],
                "dest": "_site",
                "groups": {
                  "main": { "dest": "main" },
                  "v1.0": { "dest": "v1.0" }
                }
              }
            }
            """, directory);
        CreateFile("tpl-v/public/main.js",
            """
            export default {
              versions: [
                { label: 'main', url: '/site/main/', moniker: 'net-main' },
                { label: 'v2.0 (stable)', url: '/site/', moniker: 'net-stable' },
                { label: 'v1.0', url: '/site/v1.0/', moniker: 'net-v1.0' },
              ],
            }
            """, directory);
        CreateFile("tpl-n/public/main.js",
            """
            export default {
              versions: [
                { label: 'main', url: '/site-navbar/main/', moniker: 'net-main' },
                { label: 'v2.0 (stable)', url: '/site-navbar/', moniker: 'net-stable' },
                { label: 'v1.0', url: '/site-navbar/v1.0/', moniker: 'net-v1.0' },
              ],
            }
            """, directory);
        CreateFile("stable/index.md",
            """
            # Stable docs

            ::: moniker range="net-stable"
            ## Stable-only feature

            Stable-only content.
            ::: moniker-end

            ::: moniker range="net-main"
            ## Main-only feature

            Main-only content.
            ::: moniker-end
            """, directory);
        CreateFile("main/index.md",
            """
            # Main docs

            ::: moniker range="net-stable"
            ## Stable-only feature

            Stable-only content.
            ::: moniker-end

            ::: moniker range="net-main"
            ## Main-only feature

            Main-only content.
            ::: moniker-end
            """, directory);
        CreateFile("main/only-main.md", "# Only in main", directory);
        CreateFile("v1.0/index.md", "# v1.0 docs", directory);
        // A page of the stable version (declared moniker net-stable) whose sections
        // are all for other versions' monikers
        CreateFile("stable/other-versions.md",
            """
            # Features from other versions

            ::: moniker range="net-main"
            ## Main-only feature

            Main-only content.
            ::: moniker-end

            ::: moniker range="net-v1.0"
            ## v1.0-only feature

            v1.0-only content.
            ::: moniker-end
            """, directory);
        await Docset.Build(configPath);

        // Simulate a build orchestration publishing the manifest at the site root
        var site = Path.Combine(directory, "_site");
        File.WriteAllText(Path.Combine(site, "versions.json"),
            """
            [
              { "label": "main", "url": "/site/main/", "moniker": "net-main" },
              { "label": "v2.0 (stable)", "url": "/site/", "moniker": "net-stable" },
              { "label": "v1.0", "url": "/site/v1.0/", "moniker": "net-v1.0" }
            ]
            """);

        // Build a site with _disableNavbar: moniker filtering still applies
        // even though the version selector is not rendered
        var navbarConfigPath = CreateFile("docfx-navbar.json", """
            {
              "build": {
                "content": [
                  { "files": [ "**/*.md" ], "src": "stable", "dest": "." },
                  { "files": [ "**/*.md" ], "src": "main", "dest": ".", "group": "main" },
                  { "files": [ "**/*.md" ], "src": "v1.0", "dest": ".", "group": "v1.0" }
                ],
                "template": [ "default", "modern", "tpl-n" ],
                "dest": "_site-navbar",
                "globalMetadata": {
                  "_disableNavbar": true
                },
                "groups": {
                  "main": { "dest": "main" },
                  "v1.0": { "dest": "v1.0" }
                }
              }
            }
            """, directory);
        await Docset.Build(navbarConfigPath);
        var navbarSite = Path.Combine(directory, "_site-navbar");

        // Build a site without the versions option: no selector is rendered and
        // pages don't request the versions.json manifest
        var plainConfigPath = CreateFile("docfx-plain.json", """
            {
              "build": {
                "content": [
                  { "files": [ "**/*.md" ], "src": "stable", "dest": "." },
                  { "files": [ "**/*.md" ], "src": "main", "dest": ".", "group": "main" },
                  { "files": [ "**/*.md" ], "src": "v1.0", "dest": ".", "group": "v1.0" }
                ],
                "template": [ "default", "modern" ],
                "dest": "_site-plain",
                "groups": {
                  "main": { "dest": "main" },
                  "v1.0": { "dest": "v1.0" }
                }
              }
            }
            """, directory);
        await Docset.Build(plainConfigPath);
        var plainSite = Path.Combine(directory, "_site-plain");
        File.WriteAllText(Path.Combine(plainSite, "versions.json"), "[]");

        await RunInBrowserAsync(
            [
                ("/site/", site),
                ("/site-navbar/", navbarSite),
                ("/site-plain/", plainSite),
            ],
            async (page, baseUrl) =>
            {
                var requestedUrls = new List<string>();
                page.Request += (_, request) => requestedUrls.Add(request.Url);

                await page.GotoAsync($"{baseUrl}/site/main/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                // The selectors render asynchronously: wait for the icon instead of
                // asserting its presence right after the page is ready
                await page.Locator("nav.navbar .bi-tag").WaitForAsync();

                // The version of the current page is marked as active
                await OpenDropdown(page, "tag");
                Assert.Equal("main", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

                // Moniker sections that don't apply to the current version are hidden
                Assert.True(await page.Locator("div[data-moniker~='net-main']").First.IsVisibleAsync());
                Assert.False(await page.Locator("div[data-moniker~='net-stable']").First.IsVisibleAsync());

                // The outline reflects the content that is visible
                var affix = await page.Locator("#affix").InnerTextAsync();
                Assert.Contains("Main-only feature", affix);
                Assert.DoesNotContain("Stable-only feature", affix);

                // Selecting a version opens the same page in that version
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "v2.0 (stable)" }).ClickAsync();
                await page.WaitForURLAsync("**/site/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "tag");
                Assert.Equal("v2.0 (stable)", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

                // Switching the version also switches the visible moniker sections and the outline
                Assert.False(await page.Locator("div[data-moniker~='net-main']").First.IsVisibleAsync());
                Assert.True(await page.Locator("div[data-moniker~='net-stable']").First.IsVisibleAsync());
                affix = await page.Locator("#affix").InnerTextAsync();
                Assert.Contains("Stable-only feature", affix);
                Assert.DoesNotContain("Main-only feature", affix);

                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "main" }).ClickAsync();
                await page.WaitForURLAsync("**/site/main/index.html");

                // The page only-main.html does not exist in v1.0, fall back to the home page of v1.0
                await page.GotoAsync($"{baseUrl}/site/main/only-main.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "tag");
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "v1.0" }).ClickAsync();
                await page.WaitForURLAsync("**/site/v1.0/");

                // The query string and the fragment of the current page are preserved exactly once
                await page.GotoAsync($"{baseUrl}/site/main/only-main.html?q=1#intro");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "tag");
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "v1.0" }).ClickAsync();
                // Poll the live page URL: independent of navigation events, and asserts
                // the query string and the fragment appear exactly once
                await page.WaitForFunctionAsync($"window.location.href === '{baseUrl}/site/v1.0/?q=1#intro'");

                // The declared moniker of the version applies even when it covers no moniker
                // of the page, so sections of other versions stay hidden
                await page.GotoAsync($"{baseUrl}/site/other-versions.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                Assert.False(await page.Locator("div[data-moniker~='net-main']").First.IsVisibleAsync());
                Assert.False(await page.Locator("div[data-moniker~='net-v1.0']").First.IsVisibleAsync());

                // Picking a moniker of the page reveals its sections and the outline
                await OpenDropdown(page, "layers");
                await page.Locator(".moniker-menu.show .dropdown-item").Filter(new() { HasTextString = "net-main" }).ClickAsync();
                Assert.True(await page.Locator("div[data-moniker~='net-main']").First.IsVisibleAsync());
                Assert.False(await page.Locator("div[data-moniker~='net-v1.0']").First.IsVisibleAsync());
                affix = await page.Locator("#affix").InnerTextAsync();
                Assert.Contains("Main-only feature", affix);
                Assert.DoesNotContain("v1.0-only feature", affix);

                // Moniker filtering works even when the navbar is disabled
                await page.GotoAsync($"{baseUrl}/site-navbar/main/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                Assert.Equal(0, await page.Locator("nav.navbar .bi-tag").CountAsync());
                Assert.True(await page.EvaluateAsync<bool>("document.getElementById('navbar') === null"));
                Assert.True(await page.Locator("div[data-moniker~='net-main']").First.IsVisibleAsync());
                Assert.False(await page.Locator("div[data-moniker~='net-stable']").First.IsVisibleAsync());

                // Without the versions option, no selector is rendered and
                // pages don't request the versions.json manifest
                await page.GotoAsync($"{baseUrl}/site-plain/main/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                Assert.Equal(0, await page.Locator("nav.navbar .bi-tag").CountAsync());
                Assert.DoesNotContain(requestedUrls, url => url.Contains("versions.json"));
            });
    }

    [Fact]
    public async Task LocaleSelectorNavigatesAndFallsBackToHomePage()
    {
        var directory = GetRandomFolder();

        // Build a multi-locale site: one group per locale in a subfolder
        var configPath = CreateFile("docfx.json", """
            {
              "build": {
                "content": [
                  { "files": [ "**/*.md" ], "src": "en", "dest": ".", "group": "en" },
                  { "files": [ "**/*.md" ], "src": "ja", "dest": ".", "group": "ja" }
                ],
                "template": [ "default", "modern", "tpl-l" ],
                "dest": "_site",
                "groups": {
                  "en": { "dest": "en" },
                  "ja": { "dest": "ja" }
                }
              }
            }
            """, directory);
        CreateFile("tpl-l/public/main.js", "export default { locales: '/site-locale/locales.json' }", directory);
        CreateFile("en/index.md", "# English docs", directory);
        CreateFile("en/only-en.md", "# Only in English", directory);
        CreateFile("ja/index.md", "# Japanese docs", directory);
        await Docset.Build(configPath);

        var site = Path.Combine(directory, "_site");
        File.WriteAllText(Path.Combine(site, "locales.json"),
            """
            [
              { "label": "English", "url": "/site-locale/en/" },
              { "label": "日本語", "url": "/site-locale/ja/" }
            ]
            """);

        await RunInBrowserAsync(
            [
                ("/site-locale/", site),
            ],
            async (page, baseUrl) =>
            {
                await page.GotoAsync($"{baseUrl}/site-locale/en/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                // The selectors render asynchronously: wait for the icon
                await page.Locator("nav.navbar .bi-translate").WaitForAsync();

                // The locale of the current page is marked as active
                await OpenDropdown(page, "translate");
                Assert.Equal("English", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

                // Selecting a locale opens the same page in that locale
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "日本語" }).ClickAsync();
                await page.WaitForURLAsync("**/site-locale/ja/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "translate");
                Assert.Equal("日本語", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

                // The page only-en.html is not translated, fall back to the home page of the locale
                await page.GotoAsync($"{baseUrl}/site-locale/en/only-en.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "translate");
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "日本語" }).ClickAsync();
                await page.WaitForURLAsync("**/site-locale/ja/");
            });
    }

    [Fact]
    public async Task SelectorsNavigateInCombinedVersionAndLocaleSite()
    {
        var directory = GetRandomFolder();

        // Build a site with one documentation set per version and locale,
        // e.g. /main/en/, /main/ja/, /v2.0/en/ and /v2.0/ja/
        var configPath = CreateFile("docfx.json", """
            {
              "build": {
                "content": [
                  { "files": [ "**/*.md" ], "src": "main/en", "dest": ".", "group": "main-en" },
                  { "files": [ "**/*.md" ], "src": "main/ja", "dest": ".", "group": "main-ja" },
                  { "files": [ "**/*.md" ], "src": "v2.0/en", "dest": ".", "group": "v2.0-en" },
                  { "files": [ "**/*.md" ], "src": "v2.0/ja", "dest": ".", "group": "v2.0-ja" }
                ],
                "template": [ "default", "modern", "tpl-c" ],
                "dest": "_site",
                "groups": {
                  "main-en": { "dest": "main/en" },
                  "main-ja": { "dest": "main/ja" },
                  "v2.0-en": { "dest": "v2.0/en" },
                  "v2.0-ja": { "dest": "v2.0/ja" }
                }
              }
            }
            """, directory);
        CreateFile("tpl-c/public/main.js",
            """
            export default {
              versions: '/site/versions.json',
              locales: '/site/locales.json',
            }
            """, directory);
        CreateFile("main/en/index.md", "# Main docs (en)", directory);
        CreateFile("main/ja/index.md", "# Main docs (ja)", directory);
        CreateFile("v2.0/en/index.md", "# v2.0 docs (en)", directory);
        CreateFile("v2.0/ja/index.md", "# v2.0 docs (ja)", directory);
        CreateFile("v2.0/en/only-v2.md", "# Only in v2.0 (en)", directory);
        await Docset.Build(configPath);

        var site = Path.Combine(directory, "_site");
        File.WriteAllText(Path.Combine(site, "versions.json"),
            """
            [
              { "label": "main", "url": "/site/main/" },
              { "label": "v2.0 (stable)", "url": "/site/v2.0/" }
            ]
            """);

        // One entry per version and locale combination, with one label per locale
        File.WriteAllText(Path.Combine(site, "locales.json"),
            """
            [
              { "label": "English", "url": "/site/main/en/" },
              { "label": "English", "url": "/site/v2.0/en/" },
              { "label": "日本語", "url": "/site/main/ja/" },
              { "label": "日本語", "url": "/site/v2.0/ja/" }
            ]
            """);

        await RunInBrowserAsync(
            [
                ("/site/", site),
            ],
            async (page, baseUrl) =>
            {
                await page.GotoAsync($"{baseUrl}/site/v2.0/en/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");

                // Entries with the same label are displayed once. The selectors render
                // asynchronously: wait for the icons instead of asserting their presence
                await page.Locator("nav.navbar .bi-tag").WaitForAsync();
                await page.Locator("nav.navbar .bi-translate").WaitForAsync();
                await OpenDropdown(page, "tag");
                Assert.Equal(2, await page.Locator(".dropdown-menu.show .dropdown-item").CountAsync());
                await page.Locator("nav.navbar .bi-tag").ClickAsync();
                await OpenDropdown(page, "translate");
                Assert.Equal(2, await page.Locator(".dropdown-menu.show .dropdown-item").CountAsync());

                // Switching the locale keeps the version segment of the URL
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "日本語" }).ClickAsync();
                await page.WaitForURLAsync("**/site/v2.0/ja/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "translate");
                Assert.Equal("日本語", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

                // Switching the version keeps the locale segment of the URL
                await OpenDropdown(page, "tag");
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "main" }).ClickAsync();
                await page.WaitForURLAsync("**/site/main/ja/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "tag");
                Assert.Equal("main", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());
                await OpenDropdown(page, "translate");
                Assert.Equal("日本語", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

                // The page only-v2.html does not exist in main, the walk-up fallback
                // preserves the locale segment of the URL
                await page.GotoAsync($"{baseUrl}/site/v2.0/en/only-v2.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "tag");
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "main" }).ClickAsync();
                await page.WaitForURLAsync("**/site/main/en/");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "translate");
                Assert.Equal("English", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());
            });
    }

    [Fact]
    public async Task MonikerSelectorPersistsSelectionAndFiltersContent()
    {
        var directory = GetRandomFolder();

        // Build a site with moniker sections on two pages, without the versions option:
        // the moniker selector is driven by the monikers found on the pages
        var configPath = CreateFile("docfx.json", """
            {
              "build": {
                "content": [{ "files": [ "**/*.md" ] }],
                "template": [ "default", "modern" ],
                "dest": "_site"
              }
            }
            """, directory);
        var monikerSections =
            """
            ::: moniker range="net-8.0"
            ## net-8.0 only feature

            Content for net-8.0.
            ::: moniker-end

            ::: moniker range="net-9.0"
            ## net-9.0 only feature

            Content for net-9.0.
            ::: moniker-end
            """;
        CreateFile("index.md", "# Home\n\n" + monikerSections, directory);
        CreateFile("other.md", "# Other\n\n" + monikerSections, directory);
        CreateFile("plain.md", "# A page without moniker sections", directory);
        await Docset.Build(configPath);
        var site = Path.Combine(directory, "_site");

        await RunInBrowserAsync([("/", site)], async (page, baseUrl) =>
        {
            await page.GotoAsync($"{baseUrl}/index.html");
            await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");

            // The moniker selector lists the monikers found on the page. It renders
            // asynchronously: wait for the icon
            await page.Locator("nav.navbar .bi-layers").WaitForAsync();
            await OpenDropdown(page, "layers");
            Assert.Equal(2, await page.Locator(".moniker-menu.show .dropdown-item").CountAsync());

            // Without a selected moniker, all sections are visible
            Assert.True(await page.Locator("div[data-moniker~='net-8.0']").First.IsVisibleAsync());
            Assert.True(await page.Locator("div[data-moniker~='net-9.0']").First.IsVisibleAsync());

            // Selecting a moniker hides the moniker sections that don't apply to it,
            // and the outline reflects the content that is visible
            await page.Locator(".moniker-menu.show .dropdown-item").Filter(new() { HasTextString = "net-9.0" }).ClickAsync();
            Assert.False(await page.Locator("div[data-moniker~='net-8.0']").First.IsVisibleAsync());
            Assert.True(await page.Locator("div[data-moniker~='net-9.0']").First.IsVisibleAsync());
            var affix = await page.Locator("#affix").InnerTextAsync();
            Assert.Contains("net-9.0 only feature", affix);
            Assert.DoesNotContain("net-8.0 only feature", affix);

            // Switching back to the initial moniker restores the content, the outline
            // and the active item of the menu
            await OpenDropdown(page, "layers");
            await page.Locator(".moniker-menu.show .dropdown-item").Filter(new() { HasTextString = "net-8.0" }).ClickAsync();
            Assert.True(await page.Locator("div[data-moniker~='net-8.0']").First.IsVisibleAsync());
            Assert.False(await page.Locator("div[data-moniker~='net-9.0']").First.IsVisibleAsync());
            affix = await page.Locator("#affix").InnerTextAsync();
            Assert.Contains("net-8.0 only feature", affix);
            Assert.DoesNotContain("net-9.0 only feature", affix);
            await OpenDropdown(page, "layers");
            Assert.Equal("net-8.0", (await page.Locator(".moniker-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

            // The selection is persisted and applied when browsing to other pages
            await page.GotoAsync($"{baseUrl}/other.html");
            await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
            Assert.True(await page.Locator("div[data-moniker~='net-8.0']").First.IsVisibleAsync());
            Assert.False(await page.Locator("div[data-moniker~='net-9.0']").First.IsVisibleAsync());
            await OpenDropdown(page, "layers");
            Assert.Equal("net-8.0", (await page.Locator(".moniker-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

            // Pages without moniker sections keep all their content and no selector
            await page.GotoAsync($"{baseUrl}/plain.html");
            await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
            Assert.Equal(0, await page.Locator("nav.navbar .bi-layers").CountAsync());
            Assert.Equal(0, await page.Locator("article div[data-moniker]").CountAsync());
        });
    }

    [Fact]
    public async Task VersionSelectorPreservesTargetOriginWhenFallingBack()
    {
        var directory = GetRandomFolder();

        // Build a versioned site whose versions are hosted on different origins:
        // the current version on http://127.0.0.1:<port>, the target version on
        // http://localhost:<port> (a different origin for the browser)
        var configPath = CreateFile("docfx.json", """
            {
              "build": {
                "content": [
                  { "files": [ "**/*.md" ], "src": "stable", "dest": "." },
                  { "files": [ "**/*.md" ], "src": "main", "dest": ".", "group": "main" },
                  { "files": [ "**/*.md" ], "src": "v1.0", "dest": ".", "group": "v1.0" }
                ],
                "template": [ "default", "modern", "tpl-x" ],
                "dest": "_site",
                "groups": {
                  "main": { "dest": "main" },
                  "v1.0": { "dest": "v1.0" }
                }
              }
            }
            """, directory);
        CreateFile("tpl-x/public/main.js", "export default { versions: '/site/versions.json' }", directory);
        CreateFile("stable/index.md", "# Stable docs", directory);
        CreateFile("main/index.md", "# Main docs", directory);
        CreateFile("v1.0/index.md", "# v1.0 docs", directory);
        CreateFile("v1.0/only-v1.md", "# Only in v1.0", directory);
        await Docset.Build(configPath);
        var site = Path.Combine(directory, "_site");

        using var currentOrigin = CreateServer([("/site/", site)], "127.0.0.1", out var currentUrl);
        using var targetOrigin = CreateServer([("/site/", site)], "localhost", out var targetUrl);

        // The main version is hosted on the other origin
        File.WriteAllText(Path.Combine(site, "versions.json"),
            $$"""
            [
              { "label": "main", "url": "{{targetUrl}}/site/main/" },
              { "label": "v1.0", "url": "/site/v1.0/" }
            ]
            """);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        // The page only-v1.html does not exist in main: the fallback probes and
        // navigates to the target origin, not to the current origin
        await page.GotoAsync($"{currentUrl}/site/v1.0/only-v1.html");
        await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
        await OpenDropdown(page, "tag");
        await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "main" }).ClickAsync();
        // Poll the live page URL instead of waiting for a navigation event
        await page.WaitForFunctionAsync($"window.location.href === '{targetUrl}/site/main/'");
        await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
    }

    [Fact]
    public async Task VersionSelectorKeepsSamePageWhenProbingIsUnavailable()
    {
        var directory = GetRandomFolder();

        // A version hosted on another origin that doesn't allow cross-origin probes
        // (no CORS): the selector can't determine whether the same page exists there,
        // so it navigates to it like a plain link instead of falling back to the home page
        var configPath = CreateFile("docfx.json", """
            {
              "build": {
                "content": [
                  { "files": [ "**/*.md" ], "src": "stable", "dest": "." },
                  { "files": [ "**/*.md" ], "src": "main", "dest": ".", "group": "main" },
                  { "files": [ "**/*.md" ], "src": "v1.0", "dest": ".", "group": "v1.0" }
                ],
                "template": [ "default", "modern", "tpl-np" ],
                "dest": "_site",
                "groups": {
                  "main": { "dest": "main" },
                  "v1.0": { "dest": "v1.0" }
                }
              }
            }
            """, directory);
        CreateFile("tpl-np/public/main.js", "export default { versions: '/site/versions.json' }", directory);
        CreateFile("stable/index.md", "# Stable docs", directory);
        CreateFile("main/index.md", "# Main docs", directory);
        CreateFile("v1.0/index.md", "# v1.0 docs", directory);
        await Docset.Build(configPath);
        var site = Path.Combine(directory, "_site");

        using var currentOrigin = CreateServer([("/site/", site)], "127.0.0.1", out var currentUrl);
        using var targetOrigin = CreateServer([("/site/", site)], "localhost", out var targetUrl, cors: false);

        File.WriteAllText(Path.Combine(site, "versions.json"),
            $$"""
            [
              { "label": "main", "url": "{{targetUrl}}/site/main/" },
              { "label": "v1.0", "url": "/site/v1.0/" }
            ]
            """);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        // index.html exists in both versions: without a usable probe the same page is
        // opened on the target origin, like a plain link to it
        await page.GotoAsync($"{currentUrl}/site/v1.0/index.html");
        await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
        await OpenDropdown(page, "tag");
        await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "main" }).ClickAsync();
        await page.WaitForFunctionAsync($"window.location.href === '{targetUrl}/site/main/index.html'");
        await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
    }

    private static async Task OpenDropdown(IPage page, string icon)
    {
        await page.Locator($"nav.navbar .bi-{icon}").ClickAsync();
        await page.Locator(".dropdown-menu.show").WaitForAsync();
    }

    private static async Task RunInBrowserAsync((string path, string root)[] mounts, Func<IPage, string, Task> verify)
    {
        using var server = CreateServer(mounts, "127.0.0.1", out var baseUrl);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        await verify(page, baseUrl);
    }

    private static HttpListener CreateServer((string path, string root)[] mounts, string host, out string baseUrl, bool cors = true)
    {
        // Reserve an ephemeral port, then release it for HttpListener
        int port;
        using (var socket = new TcpListener(IPAddress.Loopback, 0))
        {
            socket.Start();
            port = ((IPEndPoint)socket.LocalEndpoint).Port;
            socket.Stop();
        }

        var server = new HttpListener();
        foreach (var (path, _) in mounts)
        {
            server.Prefixes.Add($"http://{host}:{port}{path}");
        }
        server.Start();
        baseUrl = $"http://{host}:{port}";

        mounts = mounts.OrderByDescending(m => m.path.Length).ToArray();
        _ = Task.Run(async () =>
        {
            while (server.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await server.GetContextAsync();
                }
                catch
                {
                    // The listener has been stopped
                    break;
                }

                try
                {
                    HandleRequest(context, mounts, cors);
                }
                catch
                {
                    try
                    {
                        context.Response.StatusCode = 500;
                        context.Response.Close();
                    }
                    catch
                    {
                        // The connection is already gone
                    }
                }
            }
        });
        return server;
    }

    private static void HandleRequest(HttpListenerContext context, (string path, string root)[] mounts, bool cors)
    {
        var absolutePath = context.Request.Url.AbsolutePath;
        var (path, root) = mounts.First(m => absolutePath.StartsWith(m.path, StringComparison.Ordinal));
        var relativePath = absolutePath[path.Length..];
        var file = Path.Combine(root, (relativePath.Length == 0 ? "index.html" : relativePath).Replace('/', Path.DirectorySeparatorChar));

        // Directory URLs are served by their index page, like static site hosts do
        if (Directory.Exists(file))
        {
            file = Path.Combine(file, "index.html");
        }

        var status = 0;
        try
        {
            var bytes = File.ReadAllBytes(file);
            status = 200;
            context.Response.StatusCode = 200;
            // The selectors probe pages of other origins with cross-origin HEAD requests,
            // including pages that turn out to be missing (404)
            if (cors)
            {
                context.Response.AddHeader("Access-Control-Allow-Origin", "*");
            }
            context.Response.ContentType = Path.GetExtension(file) switch
            {
                ".html" => "text/html",
                ".js" => "text/javascript",
                ".css" => "text/css",
                ".json" => "application/json",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream",
            };

            if (context.Request.HttpMethod == "HEAD")
            {
                // HEAD requests must not have a body
                context.Response.ContentLength64 = bytes.Length;
            }
            else
            {
                context.Response.OutputStream.Write(bytes);
            }
        }
        catch
        {
            status = 404;
            context.Response.StatusCode = 404;
            if (cors)
            {
                context.Response.AddHeader("Access-Control-Allow-Origin", "*");
            }
        }
        // The origin in the log identifies which server served the request
        Console.WriteLine($"SERVER {context.Request.Url.Authority} {context.Request.HttpMethod} {absolutePath} -> {status}");
        context.Response.Close();
    }
}
