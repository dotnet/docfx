using System.Text.Json;
using ECMA2Yaml;
using Mono.Documentation;

var configJson = File.ReadAllText("docfx.json");
var config = JsonSerializer.Deserialize<DocfxConfig>(configJson, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

var tempDirectory = Path.GetFullPath("obj/dotnet");
var dllDirectory = Path.Combine(tempDirectory, "dll");
var latestDllDirectory = Path.Combine(dllDirectory, "latest");
var xmlDirectory = Path.Combine(tempDirectory, "xml");
var ymlDirectory = Path.GetFullPath(config.Dotnet.Dest);

if (Directory.Exists(tempDirectory))
{
    Directory.Delete(tempDirectory, true);
}

Directory.CreateDirectory(latestDllDirectory);
Directory.CreateDirectory(xmlDirectory);

foreach (var assembly in config.Dotnet.Assemblies)
{
    var src = Path.GetFullPath(assembly);
    var srcTarget = Path.Combine(latestDllDirectory, Path.GetFileName(assembly));

    Console.WriteLine($"Copy {src} --> {srcTarget}");
    File.Copy(src, srcTarget, overwrite: true);

    var xml = Path.ChangeExtension(src, ".xml");
    if (File.Exists(xml))
    {
        var xmlTarget = Path.Combine(latestDllDirectory, Path.GetFileName(xml));

        Console.WriteLine($"Copy {xml} --> {xmlTarget}");
        File.Copy(xml, xmlTarget, overwrite: true);
    }
}

new MDocFrameworksBootstrapper().Run(new[] { "fx-bootstrap", dllDirectory });

new MDocUpdater().Run(new[]
{
    "update",
    "-o", xmlDirectory,
    "-fx", dllDirectory,
    "-lang", "docid",
    "-index", "false",
    "--debug", "--delete",
});

ECMA2YamlConverter.Run(xmlDirectory, outputDirectory: ymlDirectory, config: new() { NoMonikers = true });

class DocfxConfig
{
    public DotnetConfig Dotnet { get; init; } = new();
}

class DotnetConfig
{
    public string Dest { get; init; } = "api";

    public string[] Assemblies { get; init; } = Array.Empty<string>();
}
