using Microsoft.DocAsCode;

await DocfxProject.Build("docfx.json", new() { Json = true });