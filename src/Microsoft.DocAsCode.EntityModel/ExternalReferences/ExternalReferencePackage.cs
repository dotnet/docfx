namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.IO.Compression;

    public class ExternalReferencePackage
    {
        private readonly string _packageFile;
        private readonly Uri _baseUri;

        public ExternalReferencePackage(string packageFile, Uri baseUri)
        {
            _packageFile = packageFile;
            _baseUri = baseUri;
        }

        public void CreatePackage(IReadOnlyList<string> apiPaths)
        {
            if (apiPaths == null)
            {
                throw new ArgumentNullException("apiPaths");
            }
            if (apiPaths.Count == 0)
            {
                throw new ArgumentException("Empty collection is not allowed.", "apiPaths");
            }
            using (var fileStream = new FileStream(_packageFile, FileMode.Create, FileAccess.ReadWrite))
            using (var zip = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                for (int i = 0; i < apiPaths.Count; i++)
                {
                    var vms = from file in Directory.GetFiles(Path.Combine(apiPaths[i], "api"), "*.yml", SearchOption.TopDirectoryOnly)
                              select YamlUtility.Deserialize<PageViewModel>(file);
                    var name = Path.GetFileName(apiPaths[i]);
                    var extRefs = (from vm in vms
                                   from extRef in ExternalReferenceConverter.ToExternalReferenceViewModel(vm, new Uri(_baseUri, name + "/"))
                                   select extRef);
                    var entry = zip.CreateEntry(string.Format("{0}.yml", name));
                    //var entry = zip.CreateEntry(string.Format("{0}.yml", i.ToString()));
                    using (var stream = entry.Open())
                    using (var sw = new StreamWriter(stream))
                    {
                        YamlUtility.Serialize(sw, extRefs);
                    }
                }
            }
        }
    }
}
