namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.IO.Compression;

    using Microsoft.DocAsCode.EntityModel.ViewModels;

    public class ExternalReferencePackageReader
    {
        private readonly string _packageFile;
        private readonly List<string> _uids;
        private readonly Dictionary<string, string> _uidEntryMap;

        public ExternalReferencePackageReader(string packageFile)
        {
            if (string.IsNullOrEmpty(packageFile))
            {
                throw new ArgumentException("package can't be null or empty", nameof(packageFile));
            }
            _packageFile = packageFile;
            using (var stream = new FileStream(_packageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                _uidEntryMap = (from entry in zip.Entries select entry.FullName).ToDictionary(entry => Path.GetFileNameWithoutExtension(entry));
                _uids = _uidEntryMap.Keys.OrderBy(s => s).ToList();
            }
        }

        public bool TryGetReference(string uid, out ReferenceViewModel vm)
        {
            vm = null;
            var index = _uids.BinarySearch(uid);
            if (index < 0)
            {
                index = ~index;
                if (index == 0)
                {
                    return false;
                }
                index--;
                var entryUid = _uids[index];
                if (!uid.StartsWith(entryUid))
                {
                    return false;
                }
            }
            var vms = GetReferenceViewModels(index);
            vm = vms.Find(item => item.Uid == uid);
            return vm != null;
        }

        private List<ReferenceViewModel> GetReferenceViewModels(int index)
        {
            using (var stream = new FileStream(_packageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            using (var entryStream = zip.GetEntry(_uidEntryMap[_uids[index]]).Open())
            using (var reader = new StreamReader(entryStream))
            {
                return YamlUtility.Deserialize<List<ReferenceViewModel>>(reader);
            }
        }
    }
}
