// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Cli;

using static Spectre.Console.AnsiConsole;

namespace Docfx;

class InitCommand : Command<InitCommandOptions>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] InitCommandOptions options)
    {
        WriteLine(
            """
            This utility will walk you through creating a docfx project.
            It only covers the most common items, and tries to guess sensible defaults.

            """);

        var name = options.Yes ? "" : Ask("Name", "mysite");
        var dotnetApi = options.Yes ? true : Confirm("Generate .NET API documentation?");
        var csprojLocation = options.Yes || !dotnetApi ? "src" : Ask(".NET projects location", "src");
        var docsLocation = options.Yes ? "docs" : Ask("Markdown docs location", "docs");
        var search = options.Yes ? true : Confirm("Enable site search?", true);
        var pdf = options.Yes ? true : Confirm("Enable PDF?", true);

        var outdir = Path.GetFullPath(options.OutputFolder ?? ".");

        var docfx = new
        {
            schema = "https://raw.githubusercontent.com/dotnet/docfx/main/schemas/docfx.schema.json",
            metadata = dotnetApi ? new[]
            {
                new
                {
                    src = new[]
                    {
                        new { src = $"../{csprojLocation}", files = new[] { "**/*.csproj" } }
                    },
                    dest = "api"
                }
            } : null,
            build = new
            {
                content = new[]
                {
                    new { files = new[] { "**/*.{md,yml}" }, exclude = new[] { "_site/**" } }
                },
                resource = new[]
                {
                    new { files = new[] { "images/**" } }
                },
                output = "_site",
                template = new[] { "default", "modern" },
                globalMetadata = new
                {
                    _appName = name,
                    _appTitle = name,
                    _enableSearch = search,
                    pdf,
                }
            }
        };

        var files = new Dictionary<string, string>
        {
            ["docfx.json"] = JsonSerializer.Serialize(docfx, new JsonSerializerOptions()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            }).Replace("\"schema\"", "\"$schema\""),

            ["toc.yml"] = dotnetApi ?
                $"""
                - name: Docs
                  href: {docsLocation}/
                - name: API
                  href: api/
                """ :
                $"""
                - name: Docs
                  href: {docsLocation}/
                """,

            ["index.md"] =
                """
                ---
                _layout: landing
                ---

                # This is the **HOMEPAGE**.

                Refer to [Markdown](http://daringfireball.net/projects/markdown/) for how to write markdown files.

                ## Quick Start Notes:

                1. Add images to the *images* folder if the file is referencing an image.
                """,

            [$"{docsLocation}/introduction.md"] =
                """
                # Introduction
                """,

            [$"{docsLocation}/getting-started.md"] =
                """
                # Getting Started
                """,

            [$"{docsLocation}/toc.yml"] =
                """
                - name: Introduction
                  href: introduction.md
                - name: Getting Started
                  href: getting-started.md
                """,
        };

        foreach (var (key, value) in files)
        {
            if (value is null)
                continue;

            var path = Path.GetFullPath(Path.Combine(outdir, key));
            var confirm = File.Exists(path)
                ? $"About to overwrite existing file [yellow]{path.EscapeMarkup()}[/] with:"
                : key.Contains("docfx.json") ? $"About to write to [yellow]{path.EscapeMarkup()}[/]:" : null;

            if (!options.Yes && confirm is not null)
            {
                WriteLine();
                if (!Confirm(
                    $"""
                    {confirm}

                    {value.EscapeMarkup()}

                    Is this OK?
                    """))
                {
                    WriteLine("Aborted.");
                    return -1;
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, value);
        }

        MarkupLineInterpolated(
            $"""

            [green]Project created at {Path.Combine(outdir)}[/]

            Run [yellow]docfx {Path.Combine(outdir, "docfx.json")} --serve[/] to launch the site.
            """);

        return 0;
    }
}
