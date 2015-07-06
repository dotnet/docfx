namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.IO.Compression;

    public class ExternalReferencePackage : IDisposable
    {
        private readonly string _packageFile;
        private readonly Uri _baseUri;
        private readonly ZipArchive _zip;

        private ExternalReferencePackage(string packageFile, Uri baseUri, bool append)
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

        public static ExternalReferencePackage Create(string packageFile, Uri baseUri)
        {
            return new ExternalReferencePackage(packageFile, baseUri, false);
        }

        public static ExternalReferencePackage Append(string packageFile, Uri baseUri)
        {
            return new ExternalReferencePackage(packageFile, baseUri, true);
        }

        public void AddProjects(IReadOnlyList<string> projectPaths)
        {
            if (projectPaths == null)
            {
                throw new ArgumentNullException("projectPaths");
            }
            if (projectPaths.Count == 0)
            {
                throw new ArgumentException("Empty collection is not allowed.", "projectPaths");
            }
            for (int i = 0; i < projectPaths.Count; i++)
            {
                var name = Path.GetFileName(projectPaths[i]);
                AddFiles(
                    string.Format("{0}.yml", name),
                    name + "/api/",
                    Directory.GetFiles(Path.Combine(projectPaths[i], "api"), "*.yml", SearchOption.TopDirectoryOnly));
            }
        }

        public void AddFiles(string entryName, string relativePath, IReadOnlyList<string> docPaths)
        {
            if (docPaths == null)
            {
                throw new ArgumentNullException("apiPaths");
            }
            if (docPaths.Count == 0)
            {
                throw new ArgumentException("Empty collection is not allowed.", "apiPaths");
            }
            var uri = string.IsNullOrEmpty(relativePath) ? _baseUri : new Uri(_baseUri, relativePath);
            var vms = from doc in docPaths
                      select YamlUtility.Deserialize<PageViewModel>(doc);
            var extRefs = from vm in vms
                          from extRef in ExternalReferenceConverter.ToExternalReferenceViewModel(vm, uri)
                          select extRef;
            ZipArchiveEntry entry = null;
            if (_zip.Mode == ZipArchiveMode.Read)
            {
                throw new InvalidOperationException("Cannot add files in read mode.");
            }
            if (_zip.Mode == ZipArchiveMode.Update)
            {
                entry = _zip.GetEntry(entryName);
            }
            if (_zip.Mode != ZipArchiveMode.Read)
            {
                entry = entry ?? _zip.CreateEntry(entryName);
            }
            using (var stream = entry.Open())
            using (var sw = new StreamWriter(stream))
            {
                YamlUtility.Serialize(sw, extRefs);
            }
        }

        public void Dispose()
        {
            _zip.Dispose();
        }
    }
}
