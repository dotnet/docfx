// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.Build.Engine;

public class TemplatePageLoader
{
    private readonly RendererLoader _rendererLoader;
    private readonly PreprocessorLoader _preprocessorLoader;

    public TemplatePageLoader(ResourceFileReader reader, DocumentBuildContext context, int maxParallelism)
    {
        _rendererLoader = new RendererLoader(reader, maxParallelism);
        _preprocessorLoader = new PreprocessorLoader(reader, context, maxParallelism);
    }

    public IEnumerable<Template> LoadAll()
    {
        foreach (var render in _rendererLoader.LoadAll())
        {
            var preprocessors = _preprocessorLoader.LoadFromRenderer(render).ToList();
            if (preprocessors.Count > 1)
            {
                Logger.Log(
                    LogLevel.Warning,
                    $"Multiple template preprocessors '{preprocessors.Select(s => s.Path).ToDelimitedString()}'(case insensitive) are found for template page '{preprocessors[0].Name}', '{preprocessors[0].Path}' is used and others are ignored.");
            }

            yield return new Template(render, preprocessors.FirstOrDefault());
        }

        foreach (var p in _preprocessorLoader.LoadStandalones())
        {
            yield return new Template(null, p);
        }
    }
}

