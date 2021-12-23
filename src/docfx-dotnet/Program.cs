using ECMA2Yaml;
using Mono.Documentation;

var dllDirectory = args.FirstOrDefault() ?? Directory.GetCurrentDirectory();
var xmlDirectory = Path.Combine(dllDirectory, "xml");
var ymlDirectory = Path.Combine(dllDirectory, "api");

new MDocFrameworksBootstrapper().Run(new[] { "fx-bootstrap", dllDirectory });

new MDocUpdater().Run(new[]
{
    "update",
    "-o", xmlDirectory,
    "-fx", dllDirectory,
    "-lang", "docid",
    "-index", "false",
    "--debug", "--delete",
    "-L", @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\PublicAssemblies",
    "-L", @"C:\Program Files (x86)\Microsoft.NET\Primary Interop Assemblies",
    "-L", @"C:\Program Files\WindowsPowerShell\Modules\PackageManagement\1.0.0.1",
    "-L", @"C:\Program Files\dotnet",
    "-lang", "vb.net",
    "-lang", "c++/cli",
    "--ang", "fsharp",
});

ECMA2YamlConverter.Run(xmlDirectory, outputDirectory: ymlDirectory);
