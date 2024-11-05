// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Common;

namespace Docfx.DataContracts.Common;

public class ExternalReferencePackageCollection : IDisposable
{
    private readonly LruList<ReferenceViewModelCacheItem> _cache = LruList<ReferenceViewModelCacheItem>.Create(0x100);

    public ExternalReferencePackageCollection(IEnumerable<string> packageFiles, int maxParallelism, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(packageFiles);

        Readers = (from file in packageFiles.AsParallel()
                                            .WithDegreeOfParallelism(maxParallelism)
                                            .WithCancellation(cancellationToken)
                                            .AsOrdered()
                   let reader = ExternalReferencePackageReader.CreateNoThrow(file)
                   where reader != null
                   select reader).ToImmutableList();
    }

    public ImmutableList<ExternalReferencePackageReader> Readers { get; }

    public bool TryGetReference(string uid, out ReferenceViewModel vm)
    {
        ReferenceViewModel result = null;
        if (_cache.TryFind(x => x.Block.TryGetValue(uid, out result), out ReferenceViewModelCacheItem ci))
        {
            vm = result;
            return true;
        }
        foreach (var reader in Readers)
        {
            var entries = reader.GetInternal(uid);
            if (entries == null)
            {
                continue;
            }
            foreach (var entry in entries)
            {
                ci = new ReferenceViewModelCacheItem(entry.PackageFile, entry.EntryName, entry.Content);
                _cache.Access(ci);
                if (ci.Block.TryGetValue(uid, out vm))
                {
                    return true;
                }
            }
        }
        vm = null;
        return false;
    }

    #region IDisposable Support

    private bool disposedValue = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (var reader in Readers)
                {
                    reader.Dispose();
                }
            }
            disposedValue = true;
        }
    }

    ~ExternalReferencePackageCollection()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    private sealed class ReferenceViewModelCacheItem : IEquatable<ReferenceViewModelCacheItem>
    {
        public ReferenceViewModelCacheItem(string packageFile, string entryName, List<ReferenceViewModel> block)
        {
            PackageFile = packageFile;
            EntryName = entryName;
            Block = block.ToImmutableDictionary(r => r.Uid);
        }

        public string PackageFile { get; }

        public string EntryName { get; }

        public ImmutableDictionary<string, ReferenceViewModel> Block { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ReferenceViewModelCacheItem);
        }

        public override int GetHashCode()
        {
            return PackageFile.GetHashCode() ^ EntryName.GetHashCode();
        }

        public bool Equals(ReferenceViewModelCacheItem other)
        {
            return other != null &&
                PackageFile == other.PackageFile &&
                EntryName == other.EntryName;
        }
    }
}
