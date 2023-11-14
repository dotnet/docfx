// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Docfx.Build.Engine;

public sealed class CompositeResourceReader : ResourceFileReader, IEnumerable<ResourceFileReader>
{
    private readonly ResourceFileReader[] _readers;

    public override string Name => "Composite";
    public override IEnumerable<string> Names { get; }
    public override bool IsEmpty { get; }

    public CompositeResourceReader(IEnumerable<ResourceFileReader> declaredReaders)
    {
        _readers = declaredReaders.ToArray();
        IsEmpty = _readers.Length == 0;
        Names = _readers.SelectMany(s => s.Names).Distinct().ToArray();
    }

    public override Stream GetResourceStream(string name)
    {
        for (var i = _readers.Length - 1; i >= 0; i--)
        {
            if (_readers[i].GetResourceStream(name) is { } result)
                return result;
        }

        return null;
    }

    public IEnumerator<ResourceFileReader> GetEnumerator() => ((IEnumerable<ResourceFileReader>)_readers).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _readers.GetEnumerator();
}
