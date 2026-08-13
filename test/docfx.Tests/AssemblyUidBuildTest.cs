// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Dotnet;
using Docfx.Tests.Common;

namespace Docfx.Tests;

/// <summary>
/// Builds a site out of two assemblies that share a namespace and are qualified by `assemblyUids`, which
/// covers what the metadata tests cannot: the `docfx.json` binding of the option, the file names the build
/// stage writes, and whether links to a UID carrying an assembly component resolve.
/// </summary>
[Collection("docfx STA")]
public class AssemblyUidBuildTest : TestBase
{
    private readonly string _projectFolder;

    public AssemblyUidBuildTest()
    {
        _projectFolder = GetRandomFolder();
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task BuildsSiteWithAssemblyQualifiedUids()
    {
        foreach (var name in new[] { "a", "b" })
        {
            var folder = Path.Combine(_projectFolder, name);
            Directory.CreateDirectory(folder);
            File.Copy("Assets/assemblyuid.csproj.sample.1", Path.Combine(folder, $"{name}.csproj"));
            File.Copy($"Assets/assemblyuid.{name}.cs.sample.1", Path.Combine(folder, "Widget.cs"));
        }

        // Both assemblies declare `Shared.Widget`, and the markdown links to each of them by UID, in the
        // shortcut form and in the xref form.
        await File.WriteAllTextAsync(Path.Combine(_projectFolder, "index.md"),
            """
            # Widgets

            @a::Shared.Widget and <xref:b::Shared.Widget> and [the method](xref:a::Shared.Widget.Do).
            """);

        var configPath = Path.Combine(_projectFolder, "docfx.json");
        await File.WriteAllTextAsync(configPath,
            """
            {
              "assemblyUids": [ "a", "b" ],
              "metadata": [
                {
                  "src": [ { "files": [ "*/*.csproj" ] } ],
                  "dest": "api",
                  "memberLayout": "separatePages"
                }
              ],
              "build": {
                "content": [ { "files": [ "index.md", "api/**.yml" ] } ],
                "template": [ "default" ],
                "dest": "_site"
              }
            }
            """);

        // Both entry points manage the log listeners themselves, so an unresolved link cannot be observed
        // through a `TestListenerScope` here. The rendered HTML says it just as well: an xref that does not
        // resolve is written out as plain text with no href at all.
        await DotnetApiCatalog.GenerateManagedReferenceYamlFiles(configPath);
        await Docset.Build(configPath);

        var site = Path.Combine(_projectFolder, "_site");

        // `::` is not legal in a file name, so it becomes `--`, both for the type pages and for the member
        // pages that `separatePages` splits out in the build stage.
        Assert.True(File.Exists(Path.Combine(site, "api", "a--Shared.Widget.html")));
        Assert.True(File.Exists(Path.Combine(site, "api", "b--Shared.Widget.html")));
        Assert.True(File.Exists(Path.Combine(site, "api", "a--Shared.Widget.Do.html")));

        // A constructor is the case where the two stages sanitize a second character, `#ctor` -> `-ctor`,
        // so it is the one that shows the file name and the href still agree.
        Assert.True(File.Exists(Path.Combine(site, "api", "a--Shared.Widget.-ctor.html")));
        var namespacePage = await File.ReadAllTextAsync(Path.Combine(site, "api", "a--Shared.html"));
        Assert.DoesNotContain("a::Shared.Widget.#ctor", namespacePage);

        // Every authored link resolves, each to its own assembly.
        var index = await File.ReadAllTextAsync(Path.Combine(site, "index.html"));
        Assert.Contains("href=\"api/a--Shared.Widget.html\"", index);
        Assert.Contains("href=\"api/b--Shared.Widget.html\"", index);
        // The member link lands on its own page and on the anchor of the member within it.
        Assert.Contains("href=\"api/a--Shared.Widget.Do.html#a__Shared_Widget_Do\"", index);

        // The references inside a page follow too: the type page links to the namespace page of its own
        // assembly, and the link reads as the namespace is declared. The UID itself still appears in
        // `data-uid`, which is where it belongs.
        var type = await File.ReadAllTextAsync(Path.Combine(site, "api", "a--Shared.Widget.html"));
        Assert.Contains("href=\"a--Shared.html\">Shared</a>", type);
        Assert.Contains("data-uid=\"a::Shared.Widget\"", type);
    }
}
