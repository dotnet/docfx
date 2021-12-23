using Mono.Documentation;

var dir = args.FirstOrDefault() ?? Directory.GetCurrentDirectory();

new MDocFrameworksBootstrapper().Run(new[] { "fx-bootstrap", dir });

new MDocUpdater().Run(new[]
{
    "update",
    "-o", Path.Combine(dir, "_xml"),
    "-fx", dir,
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
