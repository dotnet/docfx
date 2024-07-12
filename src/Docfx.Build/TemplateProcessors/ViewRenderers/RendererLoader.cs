// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

using Docfx.Common;

namespace Docfx.Build.Engine;

public class RendererLoader
{
    private readonly ResourceFileReader _reader;
    private readonly int _maxParallelism;

    public RendererLoader(ResourceFileReader reader, int maxParallelism)
    {
        _reader = reader;
        _maxParallelism = maxParallelism;
    }

    public IEnumerable<ITemplateRenderer> LoadAll()
    {
        // Only files under root folder are allowed
        foreach (var res in _reader.GetResources($"^[^/]*({Regex.Escape(MustacheTemplateRenderer.Extension)})$"))
        {
            var renderer = Load(res);
            if (renderer != null)
            {
                yield return renderer;
            }
        }
    }

    public ITemplateRenderer Load(string path)
    {
        var content = _reader.GetResource(path);
        if (content == null)
        {
            return null;
        }

        return Load(new ResourceInfo(path, content));
    }

    public ITemplateRenderer Load(ResourceInfo res)
    {
        if (res == null)
        {
            return null;
        }

        using (new LoggerFileScope(res.Path))
        {
            var extension = Path.GetExtension(res.Path);
            if (extension.Equals(MustacheTemplateRenderer.Extension, System.StringComparison.OrdinalIgnoreCase))
            {
                return new RendererWithResourcePool(() => new MustacheTemplateRenderer(_reader, res), _maxParallelism);
            }
            else
            {
                Logger.LogWarning($"{res.Path} is not a supported template view.");
                return null;
            }
        }
    }
}

