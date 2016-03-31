namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    internal static class ExternalReferencePackageHelper
    {

        public static void AddFiles(ExternalReferencePackageWriter writer, Uri baseUri, string urlPattern, string relativePath, IEnumerable<string> docPaths)
        {
            var uri = string.IsNullOrEmpty(relativePath) ? baseUri : new Uri(baseUri, relativePath);
            foreach (var item in from doc in docPaths
                                 let vm = LoadViewModelNoThrow(doc)
                                 where vm != null
                                 let extRef = ToExternalReferenceViewModel(vm.Item2, uri, urlPattern).ToList()
                                 select new { EntryName = vm.Item1, Refs = extRef })
            {
                writer.AddOrUpdateEntry(item.EntryName, item.Refs);
            }
        }

        private static IEnumerable<ReferenceViewModel> ToExternalReferenceViewModel(PageViewModel page, Uri baseUri, string urlPattern)
        {
            foreach (var item in page.Items)
            {
                var vm = new ReferenceViewModel
                {
                    Uid = item.Uid,
                    Name = item.Name,
                    FullName = item.FullName,
                    Href = urlPattern.Replace("{baseUri}", baseUri.ToString()).Replace("{fileName}", Path.GetFileNameWithoutExtension(item.Href))
                };
                foreach (var pair in item.Names)
                {
                    vm.NameInDevLangs[pair.Key] = pair.Value;
                }
                foreach (var pair in item.FullNames)
                {
                    vm.FullNameInDevLangs[pair.Key] = pair.Value;
                }
                yield return vm;
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
    }
}
