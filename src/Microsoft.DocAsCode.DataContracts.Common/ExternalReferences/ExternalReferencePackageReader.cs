// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.IO.Compression;

    using Microsoft.DocAsCode.Common;

    using YamlDotNet.Core;

    public class ExternalReferencePackageReader : IDisposable
    {
        private readonly string _packageFile;
        private readonly List<string> _uids;
        private readonly Dictionary<string, List<string>> _uidEntryMap;
        private readonly ZipArchive _zip;

        public ExternalReferencePackageReader(string packageFile)
        {
            if (string.IsNullOrEmpty(packageFile))
            {
                throw new ArgumentException("package can't be null or empty", nameof(packageFile));
            }
            if (!File.Exists(packageFile))
            {
                throw new FileNotFoundException("Package not found.", packageFile);
            }
            _packageFile = packageFile;
            var stream = new FileStream(packageFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            _zip = new ZipArchive(stream, ZipArchiveMode.Read);
            _uidEntryMap = GetUidEntryMap(_zip);
            _uids = _uidEntryMap.Keys.OrderBy(s => s).ToList();
        }

        public static ExternalReferencePackageReader CreateNoThrow(string packageFile)
        {
            try
            {
                return new ExternalReferencePackageReader(packageFile);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool TryGetReference(string uid, out ReferenceViewModel vm)
        {
            vm = null;
            var entries = GetInternal(uid);
            if (entries == null)
            {
                return false;
            }
            vm = (from entry in entries
                  from item in entry.Content
                  where item.Uid == uid
                  select item).FirstOrDefault();
            return vm != null;
        }

        private static Dictionary<string, List<string>> GetUidEntryMap(ZipArchive zip)
        {
            var uidEntryMap = new Dictionary<string, List<string>>();
            var entries = from entry in zip.Entries
                          where !string.IsNullOrEmpty(entry.Name) && entry.Length > 0
                          select new
                          {
                              Uid = Path.GetFileNameWithoutExtension(entry.Name),
                              FullName = entry.FullName,
                          };
            foreach (var entry in entries)
            {
                if (!uidEntryMap.TryGetValue(entry.Uid, out List<string> list))
                {
                    list = new List<string>();
                    uidEntryMap[entry.Uid] = list;
                }
                list.Add(entry.FullName);
            }
            return uidEntryMap;
        }

        protected virtual int SeekUidIndex(string uid)
        {
            var searchUid = uid;
            while (true)
            {
                int index = _uids.BinarySearch(searchUid);
                if (index >= 0)
                {
                    return index;
                }
                var indexOfDot = searchUid.LastIndexOf('.');
                if (indexOfDot == -1)
                {
                    return index;
                }
                searchUid = searchUid.Remove(indexOfDot);
            }
        }

        private IEnumerable<PackageEntry> GetReferenceViewModels(int index)
        {
            var entries = _uidEntryMap[_uids[index]];
            foreach (var entry in entries)
            {
                List<ReferenceViewModel> vms = null;
                using (var entryStream = _zip.GetEntry(entry).Open())
                using (var reader = new StreamReader(entryStream))
                {
                    try
                    {
                        vms = YamlUtility.Deserialize<List<ReferenceViewModel>>(reader);
                    }
                    catch (YamlException)
                    {
                        // Ignore non-yaml entries
                    }
                }
                if (vms != null)
                {
                    yield return new PackageEntry(_packageFile, entry, vms);
                }
            }
        }

        internal IEnumerable<PackageEntry> GetInternal(string uid)
        {
            int index = SeekUidIndex(uid);
            if (index < 0)
            {
                return null;
            }
            return GetReferenceViewModels(index);
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _zip.Dispose();
                }
                disposedValue = true;
            }
        }

        ~ExternalReferencePackageReader()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        internal sealed class PackageEntry
        {
            public PackageEntry(string packageFile, string entryName, List<ReferenceViewModel> content)
            {
                PackageFile = packageFile;
                EntryName = entryName;
                Content = content;
            }

            public string PackageFile { get; }

            public string EntryName { get; }

            public List<ReferenceViewModel> Content { get; }
        }
    }
}
