// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
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
                case DocumentType.Override:
                    if (".md".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
                    {
                        return ProcessingPriority.Normal;
                    }
                    break;
                default:
                    break;
            }
            return ProcessingPriority.NotSupportted;
        }

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    var page = YamlUtility.Deserialize<PageViewModel>(Path.Combine(file.BaseDir, file.File));
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
                    var result = new FileModel(file, page, serializer: new BinaryFormatter())
                    {
                        Uids = (from item in page.Items select item.Uid).ToImmutableArray(),
                    };
                    result.Properties.LinkToFiles = new HashSet<string>();
                    result.Properties.LinkToUids = new HashSet<string>();
                    return result;
                case DocumentType.Override:
                    var overrides = MarkdownReader.ReadMarkdownAsOverride(file.BaseDir, file.File);
                    return new FileModel(file, overrides, serializer: new BinaryFormatter())
                    {
                        Uids = (from item in overrides
                                select item.Uid).ToImmutableArray(),
                        Properties =
                        {
                            LinkToFiles = new HashSet<string>(),
                            LinkToUids = new HashSet<string>(),
                        }
                    };
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

            return new SaveResult
            {
                DocumentType = "ManagedReference",
                ModelFile = model.File,
                LinkToFiles = ((HashSet<string>)model.Properties.LinkToFiles).ToImmutableArray(),
                LinkToUids = ((IEnumerable<string>)model.Properties.LinkToUids).ToDictionary(s => s, s => new HashSet<string> { model.LocalPathFromRepoRoot }).ToImmutableDictionary(),
                XRefSpecs = (from item in vm.Items
                             select GetXRefInfo(item, model.OriginalFileAndType.File)).ToImmutableArray(),
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
                if (reference.SpecForCSharp == null && reference.SpecForVB == null)
                {
                    yield return reference.Uid;
                }
                else
                {
                    // for spec type, only return the real type:
                    // e.g.:
                    //  - List<string>  -->  List`1, string
                    //  - object[]      -->  object
                    if (reference.SpecForCSharp != null)
                    {
                        foreach (var specItem in reference.SpecForCSharp)
                        {
                            if (specItem.Uid != null)
                            {
                                yield return specItem.Uid;
                            }
                        }
                    }
                    if (reference.SpecForVB != null)
                    {
                        foreach (var specItem in reference.SpecForVB)
                        {
                            if (specItem.Uid != null)
                            {
                                yield return specItem.Uid;
                            }
                        }
                    }
                }
            }
        }

        private static XRefSpec GetXRefInfo(ItemViewModel item, string href)
        {
            var result = new XRefSpec
            {
                Uid = item.Uid,
                Name = item.Name,
                Href = ((RelativePath)href).GetPathFromWorkingFolder(),
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
