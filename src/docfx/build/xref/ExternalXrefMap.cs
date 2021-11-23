// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class ExternalXrefMap
{
    private readonly Dictionary<string, Lazy<ExternalXrefSpec>> _xrefSpecs;
    private readonly List<ExternalXref> _externalXrefs;

    public ExternalXrefMap(Dictionary<string, Lazy<ExternalXrefSpec>> xrefSpecs, List<ExternalXref> externalXrefs)
    {
        _xrefSpecs = xrefSpecs;
        _externalXrefs = externalXrefs;
    }

    public ExternalXrefMap(IEnumerable<ExternalXrefMap> xrefMaps)
    {
        _externalXrefs = new();
        _xrefSpecs = new();

        foreach (var xrefMap in xrefMaps)
        {
            foreach (var (key, value) in xrefMap._xrefSpecs)
            {
                _xrefSpecs.TryAdd(key, value);
            }

            foreach (var externalXref in xrefMap._externalXrefs)
            {
                _externalXrefs.Add(externalXref);
            }
        }
    }

    public bool TryGetValue(string uid, out ExternalXrefSpec? spec)
    {
        var result = _xrefSpecs.TryGetValue(uid, out var lazySpec);
        spec = lazySpec?.Value;
        return result;
    }

    public IEnumerable<ExternalXref> GetExternalXrefs()
    {
        return _externalXrefs;
    }
}
