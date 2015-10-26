// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.IO.Compression;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using YamlDotNet.Core;

    public class ExternalReferencePackageReader
    {
        private readonly string _packageFile;
        private readonly List<string> _uids;
        private readonly Dictionary<string, List<string>> _uidEntryMap;
        private readonly Dictionary<string, List<ReferenceViewModel>> _cache;

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
            _uidEntryMap = GetUidEntryMap(packageFile);
            _uids = _uidEntryMap.Keys.OrderBy(s => s).ToList();
            _cache = new Dictionary<string, List<ReferenceViewModel>>();
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
            int index = SeekUidIndex(uid);
            if (index < 0)
            {
                return false;
            }
            vm = (from vms in GetReferenceViewModels(index)
                  from item in vms
                  where item.Uid == uid
                  select item).FirstOrDefault();
            return vm != null;
        }

        private static Dictionary<string, List<string>> GetUidEntryMap(string packageFile)
        {
            using (var stream = new FileStream(packageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
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
                    List<string> list;
                    if (!uidEntryMap.TryGetValue(entry.Uid, out list))
                    {
                        list = new List<string>();
                        uidEntryMap[entry.Uid] = list;
                    }
                    list.Add(entry.FullName);
                }
                return uidEntryMap;
            }
        }

        private int SeekUidIndex(string uid)
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

        private IEnumerable<List<ReferenceViewModel>> GetReferenceViewModels(int index)
        {
            using (var stream = new FileStream(_packageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var entries = _uidEntryMap[_uids[index]];
                foreach (var entry in entries)
                {
                    List<ReferenceViewModel> vms = null;
                    if (!_cache.TryGetValue(entry, out vms))
                    {
                        using (var entryStream = zip.GetEntry(entry).Open())
                        using (var reader = new StreamReader(entryStream))
                        {
                            try
                            {
                                vms = _cache[entry] = YamlUtility.Deserialize<List<ReferenceViewModel>>(reader);
                            }
                            catch (YamlException)
                            {
                                // Ignore non-yaml entries
                            }
                        }
                    }

                    if (vms != null)
                    {
                        yield return vms;
                    }
                }
            }
        }
    }
}
