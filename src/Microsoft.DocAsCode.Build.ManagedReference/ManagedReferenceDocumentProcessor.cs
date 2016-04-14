// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(typeof(IDocumentProcessor))]
    public class ManagedReferenceDocumentProcessor : DisposableDocumentProcessor
    {
        [ImportMany(nameof(ManagedReferenceDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override string Name => nameof(ManagedReferenceDocumentProcessor);

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    if (".yml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
                    {
                        return ProcessingPriority.Normal;
                    }

                    if (".csyml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase) ||
                        ".csyaml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
                    {
                        return ProcessingPriority.Normal;
                    }
                    break;
                case DocumentType.Overwrite:
                    if (".md".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
                    {
                        return ProcessingPriority.Normal;
                    }
                    break;
                default:
                    break;
            }
            return ProcessingPriority.NotSupported;
        }

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    var page = YamlUtility.Deserialize<PageViewModel>(Path.Combine(file.BaseDir, file.File));
                    if (page.Items == null || page.Items.Count == 0)
                    {
                        return null;
                    }
                    if (page.Metadata == null)
                    {
                        page.Metadata = metadata.ToDictionary(p => p.Key, p => p.Value);
                    }
                    else
                    {
                        foreach (var item in metadata)
                        {
                            if (!page.Metadata.ContainsKey(item.Key))
                            {
                                page.Metadata[item.Key] = item.Value;
                            }
                        }
                    }
                    var filePath = Path.Combine(file.BaseDir, file.File);
                    var repoInfo = GitUtility.GetGitDetail(filePath);
                    // Item's source is the path for the original code, should not be used here
                    var displayLocalPath = repoInfo?.RelativePath ?? filePath.ToDisplayPath();
                    return new FileModel(file, page, serializer: new BinaryFormatter())
                    {
                        Uids = (from item in page.Items select new UidDefinition(item.Uid, displayLocalPath)).ToImmutableArray(),

                        Properties =
                        {
                            LinkToFiles = new HashSet<string>(),
                            LinkToUids = new HashSet<string>(),
                        },
                        LocalPathFromRepoRoot = displayLocalPath,
                    };
                case DocumentType.Overwrite:
                    // TODO: Refactor current behavior that overwrite file is read multiple times by multiple processors
                    return OverwriteDocumentReader.Read(file);
                default:
                    throw new NotSupportedException();
            }
        }

        public override SaveResult Save(FileModel model)
        {
            if (model.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }
            var vm = (PageViewModel)model.Content;

            model.Content = ApiBuildOutput.FromModel(vm); // Fill in details

            return new SaveResult
            {
                DocumentType = "ManagedReference",
                FileWithoutExtension = Path.ChangeExtension(model.File, null),
                LinkToFiles = ((HashSet<string>)model.Properties.LinkToFiles).ToImmutableArray(),
                LinkToUids = ((HashSet<string>)model.Properties.LinkToUids).ToImmutableHashSet(),
                XRefSpecs = (from item in vm.Items
                             select GetXRefInfo(item, model.Key)).ToImmutableArray(),
            };
        }

        private IEnumerable<string> GetViewModelXRef(PageViewModel vm)
        {
            foreach (var reference in vm.References)
            {
                if (reference.Uid.StartsWith("{") && reference.Uid.EndsWith("}"))
                {
                    // ignore generic type parameter.
                    continue;
                }

                List<SpecViewModel> result;

                // for spec type, only return the real type:
                // e.g.:
                //  - List<string>  -->  List`1, string
                //  - object[]      -->  object
                if (reference.Specs.TryGetValue("csharp", out result))
                {
                    foreach (var specItem in result)
                    {
                        if (specItem.Uid != null)
                        {
                            yield return specItem.Uid;
                        }
                    }
                }

                if (reference.Specs.TryGetValue("vb", out result))
                {
                    foreach (var specItem in result)
                    {
                        if (specItem.Uid != null)
                        {
                            yield return specItem.Uid;
                        }
                    }
                }

                if (result == null)
                {
                    yield return reference.Uid;
                }
            }
        }

        private static XRefSpec GetXRefInfo(ItemViewModel item, string key)
        {
            var result = new XRefSpec
            {
                Uid = item.Uid,
                Name = item.Name,
                Href = key,
            };
            if (!string.IsNullOrEmpty(item.NameForCSharp))
            {
                result["name.csharp"] = item.NameForCSharp;
            }
            if (!string.IsNullOrEmpty(item.NameForVB))
            {
                result["name.vb"] = item.NameForVB;
            }
            if (!string.IsNullOrEmpty(item.FullName))
            {
                result["fullName"] = item.FullName;
            }
            if (!string.IsNullOrEmpty(item.FullNameForCSharp))
            {
                result["fullName.csharp"] = item.FullNameForCSharp;
            }
            if (!string.IsNullOrEmpty(item.FullNameForVB))
            {
                result["fullName.vb"] = item.FullNameForVB;
            }
            return result;
        }
    }
}
