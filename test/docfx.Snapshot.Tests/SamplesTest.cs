// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Docfx.Dotnet;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Actions;
using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Outline;

namespace Docfx.Tests;

[Trait("Stage", "Snapshot")]
public class SamplesTest
{
    private class SnapshotFactAttribute : FactAttribute
    {
        public SnapshotFactAttribute()
        {
            Skip = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SNAPSHOT_TEST")) ? "Skip snapshot tests" : null;
        }
    }

    private static readonly string s_samplesDir = Path.GetFullPath("../../../../../samples");

    static SamplesTest()
    {
        Process.Start("dotnet", $"build \"{s_samplesDir}/seed/dotnet/assembly/BuildFromAssembly.csproj\"").WaitForExit();
    }

    [SnapshotFact]
    public async Task Seed()
    {
        var samplePath = $"{s_samplesDir}/seed";
        Clean(samplePath);

        if (Debugger.IsAttached)
        {
            Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", "main");
            Assert.Equal(0, Program.Main(new[] { $"{samplePath}/docfx.json" }));
        }
        else
        {
            var docfxPath = Path.GetFullPath(OperatingSystem.IsWindows() ? "docfx.exe" : "docfx");
            Assert.Equal(0, Exec(docfxPath, $"{samplePath}/docfx.json"));
        }

        Parallel.ForEach(Directory.EnumerateFiles($"{samplePath}/_site", "*.pdf", SearchOption.AllDirectories), PdfToJson);

        await VerifyDirectory($"{samplePath}/_site", IncludeFile, fileScrubber: ScrubFile).UniqueForOSPlatform().AutoVerify(includeBuildServer: false);

        void PdfToJson(string path)
        {
            using var document = PdfDocument.Open(path);

            var pdf = new
            {
                document.NumberOfPages,
                Pages = document.GetPages().Select(p => new
                {
                    p.Number,
                    p.NumberOfImages,
                    p.Text,
                    Links = p.ExperimentalAccess.GetAnnotations().Select(ToLink).ToArray(),
                }).ToArray(),
                Bookmarks = document.TryGetBookmarks(out var bookmarks) ? ToBookmarks(bookmarks.Roots) : null,
            };

            var json = JsonSerializer.Serialize(pdf, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            });

            File.WriteAllText(Path.ChangeExtension(path, ".pdf.json"), json);

            object ToLink(Annotation a) => a.Action switch
            {
                GoToAction g => new { Goto = g.Destination },
                UriAction u => new { u.Uri },
            };

            object ToBookmarks(IEnumerable<BookmarkNode> nodes)
            {
                return nodes.Select(node => node switch
                {
                    DocumentBookmarkNode d => (object)new { node.Title, Children = ToBookmarks(node.Children), d.Destination },
                    UriBookmarkNode d => new { node.Title, Children = ToBookmarks(node.Children), d.Uri },
                }).ToArray();
            }
        }
    }

    [SnapshotFact]
    public async Task SeedMarkdown()
    {
        var samplePath = $"{s_samplesDir}/seed";
        var outputPath = nameof(SeedMarkdown);
        Clean(samplePath);

        Program.Main(new[] { "metadata", $"{samplePath}/docfx.json", "--outputFormat", "markdown", "--output", outputPath });

        await VerifyDirectory(outputPath, IncludeFile, fileScrubber: ScrubFile).UniqueForOSPlatform().AutoVerify(includeBuildServer: false);
    }

    [SnapshotFact]
    public async Task CSharp()
    {
        var samplePath = $"{s_samplesDir}/csharp";
        Clean(samplePath);

        Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", "main");

        try
        {
            await DotnetApiCatalog.GenerateManagedReferenceYamlFiles($"{samplePath}/docfx.json");
            await Docset.Build($"{samplePath}/docfx.json");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", null);
        }

        await VerifyDirectory($"{samplePath}/_site", IncludeFile).UniqueForOSPlatform().AutoVerify(includeBuildServer: false);
    }

    [SnapshotFact]
    public Task Extensions()
    {
        var samplePath = $"{s_samplesDir}/extensions";
        Clean(samplePath);

#if DEBUG
        Assert.Equal(0, Exec("dotnet", "run --no-build --project build", workingDirectory: samplePath));
#else
        Assert.Equal(0, Exec("dotnet", "run --no-build -c Release --project build", workingDirectory: samplePath));
#endif

        return VerifyDirectory($"{samplePath}/_site", IncludeFile).UniqueForOSPlatform().AutoVerify(includeBuildServer: false);
    }

    private static int Exec(string filename, string args, string workingDirectory = null)
    {
        var psi = new ProcessStartInfo(filename, args);
        psi.EnvironmentVariables.Add("DOCFX_SOURCE_BRANCH_NAME", "main");
        if (workingDirectory != null)
            psi.WorkingDirectory = Path.GetFullPath(workingDirectory);
        var process = Process.Start(psi);
        process.WaitForExit();
        return process.ExitCode;
    }

    private static void Clean(string samplePath)
    {
        if (Directory.Exists($"{samplePath}/_site"))
            Directory.Delete($"{samplePath}/_site", recursive: true);

        if (Directory.Exists($"{samplePath}/_site_pdf"))
            Directory.Delete($"{samplePath}/_site_pdf", recursive: true);
    }

    private static bool IncludeFile(string file)
    {
        return Path.GetExtension(file) switch
        {
            ".json" => Path.GetFileName(file) != "manifest.json",
            ".yml" or ".md" => true,
            _ => false,
        };
    }

    private void ScrubFile(string path, StringBuilder builder)
    {
        if (Path.GetExtension(path) == ".json" && JsonNode.Parse(builder.ToString()) is JsonObject obj)
        {
            obj.Remove("__global");
            obj.Remove("_systemKeys");
            builder.Clear();
            builder.Append(JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }));
        }
    }
}
