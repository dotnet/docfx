using Docfx;
using Docfx.Dotnet;
using Markdig;

await DotnetApiCatalog.GenerateManagedReferenceYamlFiles("docfx.json", new()
{
    // Exclude API named "MyField"
    IncludeApi = symbol => symbol.Name is "MyField" ? SymbolIncludeState.Exclude : default,
});

await Docset.Build("docfx.json", new()
{
    // Enable citation markdown extension
    ConfigureMarkdig = pipeline => pipeline.UseCitations(),
});
