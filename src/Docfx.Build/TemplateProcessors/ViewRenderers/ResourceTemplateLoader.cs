// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Stubble.Core.Interfaces;

namespace Docfx.Build.Engine;

internal sealed class ResourceTemplateLoader : IStubbleLoader
{
    private const string PartialTemplateExtension = ".tmpl.partial";
    private readonly ConcurrentDictionary<string, string> _templateCache = new();
    private readonly ResourceFileReader _reader;

    public ResourceTemplateLoader(ResourceFileReader reader)
    {
        _reader = reader;
    }

    public string Load(string name)
    {
        if (_reader == null) return null;
        var resourceName = name + PartialTemplateExtension;

        return _templateCache.GetOrAdd(resourceName, s =>
        {
            lock (_reader)
            {
                using var stream = _reader.GetResourceStream(s);
                if (stream == null) return null;

                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        });
    }

    public ValueTask<string> LoadAsync(string name)
    {
        return new ValueTask<string>(Load(name));
    }

    public IStubbleLoader Clone()
    {
        return new ResourceTemplateLoader(_reader);
    }
}
