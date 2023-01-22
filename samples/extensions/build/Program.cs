using Microsoft.DocAsCode;
using Markdig;

var options = new BuildOptions
{
    // Enable citation markdown extension
    ConfigureMarkdig = pipeline => pipeline.UseCitations(),
};

await Docset.Build("docfx.json", options);
