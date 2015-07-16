namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.IO.Compression;

    using Microsoft.DocAsCode.EntityModel.ViewModels;

    public class ExternalReferencePackageWriter : IDisposable
    {
        private readonly string _packageFile;
        private readonly Uri _baseUri;
        private readonly ZipArchive _zip;

        private ExternalReferencePackageWriter(string packageFile, Uri baseUri, bool append)
        {
            _packageFile = packageFile;
            _baseUri = baseUri;
            if (append && File.Exists(packageFile))
            {
                _zip = new ZipArchive(new FileStream(_packageFile, FileMode.Open, FileAccess.ReadWrite), ZipArchiveMode.Update);
            }
            else
            {
                _zip = new ZipArchive(new FileStream(_packageFile, FileMode.Create, FileAccess.ReadWrite), ZipArchiveMode.Create);
            }
        }

        public static ExternalReferencePackageWriter Create(string packageFile, Uri baseUri)
        {
            return new ExternalReferencePackageWriter(packageFile, baseUri, false);
        }

        public static ExternalReferencePackageWriter Append(string packageFile, Uri baseUri)
        {
            return new ExternalReferencePackageWriter(packageFile, baseUri, true);
        }

        public void AddProjects(IReadOnlyList<string> projectPaths)
        {
            if (projectPaths == null)
            {
                throw new ArgumentNullException(nameof(projectPaths));
            }
            if (projectPaths.Count == 0)
            {
                throw new ArgumentException("Empty collection is not allowed.", nameof(projectPaths));
            }
            for (int i = 0; i < projectPaths.Count; i++)
            {
                var name = Path.GetFileName(projectPaths[i]);
                AddFiles(
                    name + "/api/",
                    Directory.GetFiles(Path.Combine(projectPaths[i], "api"), "*.yml", SearchOption.TopDirectoryOnly));
            }
        }

        public void AddFiles(string relativePath, IReadOnlyList<string> docPaths)
        {
            if (docPaths == null)
            {
                throw new ArgumentNullException(nameof(docPaths));
            }
            if (docPaths.Count == 0)
            {
                throw new ArgumentException("Empty collection is not allowed.", nameof(docPaths));
            }
            var uri = string.IsNullOrEmpty(relativePath) ? _baseUri : new Uri(_baseUri, relativePath);
            foreach (var item in from doc in docPaths
                                 let vm = LoadViewModelNoThrow(doc)
                                 where vm != null
                                 let extRef = ExternalReferenceConverter.ToExternalReferenceViewModel(vm.Item2, uri).ToList()
                                 select new { EntryName = vm.Item1, Refs = extRef })
            {
                ZipArchiveEntry entry = null;
                if (_zip.Mode == ZipArchiveMode.Update)
                {
                    entry = _zip.GetEntry(item.EntryName);
                }
                entry = entry ?? _zip.CreateEntry(item.EntryName);
                using (var stream = entry.Open())
                using (var sw = new StreamWriter(stream))
                {
                    YamlUtility.Serialize(sw, item.Refs);
                }
            }
        }

        public void AddOrUpdateEntry(string entryName, List<ReferenceViewModel> vm)
        {
            if (entryName == null)
            {
                throw new ArgumentNullException(nameof(entryName));
            }
            if (vm == null)
            {
                throw new ArgumentNullException(nameof(vm));
            }
            if (vm.Count == 0)
            {
                throw new ArgumentException("Empty collection is not allowed.", nameof(vm));
            }
            ZipArchiveEntry entry = null;
            if (_zip.Mode == ZipArchiveMode.Update)
            {
                entry = _zip.GetEntry(entryName);
            }
            entry = entry ?? _zip.CreateEntry(entryName);
            using (var stream = entry.Open())
            using (var sw = new StreamWriter(stream))
            {
                YamlUtility.Serialize(sw, vm);
            }
        }

        private static Tuple<string, PageViewModel> LoadViewModelNoThrow(string filePath)
        {
            try
            {
                return Tuple.Create(Path.GetFileName(filePath), YamlUtility.Deserialize<PageViewModel>(filePath));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Dispose()
        {
            _zip.Dispose();
        }
    }
}
