using Microsoft.DocAsCode;
using Microsoft.DocAsCode.Dotnet;
using Markdig;

var options = new BuildOptions
{
    // Enable citation markdown extension
    ConfigureMarkdig = pipeline => pipeline.UseCitations(),
};

await DotnetApiCatalog.GenerateManagedReferenceYamlFiles("docfx.json");
await Docset.Build("docfx.json", options);
