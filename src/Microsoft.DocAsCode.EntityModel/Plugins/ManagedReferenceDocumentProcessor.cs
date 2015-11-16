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

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;
    using Microsoft.DocAsCode.Utility.EntityMergers;

    [Export(typeof(IDocumentProcessor))]
    public class ManagedReferenceDocumentProcessor : IDocumentProcessor
    {
        private readonly ReflectionEntityMerger Merger = new ReflectionEntityMerger();

        public string Name => nameof(ManagedReferenceDocumentProcessor);

        public ProcessingPriority GetProcessingPriority(FileAndType file)
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

        public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
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

        public SaveResult Save(FileModel model)
        {
            if (model.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }
            var vm = (PageViewModel)model.Content;

            JsonUtility.Serialize(Path.Combine(model.BaseDir, model.File), vm);
            return new SaveResult
            {
                DocumentType = "ManagedReference",
                ModelFile = model.File,
                LinkToFiles = ((HashSet<string>)model.Properties.LinkToFiles).ToImmutableArray(),
                LinkToUids = ((HashSet<string>)model.Properties.LinkToUids).ToImmutableArray(),
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

        public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            return models;
        }

        public void Build(FileModel model, IHostService host)
        {
            switch (model.Type)
            {
                case DocumentType.Article:
                    var page = (PageViewModel)model.Content;
                    foreach (var item in page.Items)
                    {
                        BuildItem(host, item, model);
                    }
                    foreach (var reference in page.References)
                    {
                        BuildReference(host, reference, model);
                    }

                    model.File = Path.ChangeExtension(model.File, ".json");
                    break;
                case DocumentType.Override:
                    foreach (var item in (List<ItemViewModel>)model.Content)
                    {
                        BuildItem(host, item, model);
                    }
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        public IEnumerable<FileModel> Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            if (models.Count > 0)
            {
                ApplyOverrides(models, host);
            }
            return models;
        }

        #region Private methods

        private void ApplyOverrides(ImmutableList<FileModel> models, IHostService host)
        {
            foreach (var uid in host.GetAllUids())
            {
                var ms = host.LookupByUid(uid);
                var od = ms.SingleOrDefault(m => m.Type == DocumentType.Override);
                if (od != null)
                {
                    var ovm = ((List<ItemViewModel>)od.Content).Single(vm => vm.Uid == uid);
                    foreach (var pair in from model in ms
                                         where model.Type == DocumentType.Article
                                         from item in ((PageViewModel)model.Content).Items
                                         where item.Uid == uid
                                         select new { model, item })
                    {
                        var vm = pair.item;
                        // todo : fix file path
                        Merger.Merge(ref vm, ovm);
                        ((HashSet<string>)pair.model.Properties.LinkToUids).UnionWith((HashSet<string>)od.Properties.LinkToUids);
                        ((HashSet<string>)pair.model.Properties.LinkToFiles).UnionWith((HashSet<string>)od.Properties.LinkToFiles);
                    }
                }
            }
        }

        private void BuildItem(IHostService host, ItemViewModel item, FileModel model)
        {
            item.Summary = Markup(host, item.Summary, model);
            item.Remarks = Markup(host, item.Remarks, model);
            item.Conceptual = Markup(host, item.Conceptual, model);
            if (item.Syntax?.Return?.Description != null)
            {
                item.Syntax.Return.Description = Markup(host, item.Syntax?.Return?.Description, model);
            }
            var parameters = item.Syntax?.Parameters;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    parameter.Description = Markup(host, parameter.Description, model);
                }
            }
            if (item.Exceptions != null)
            {
                foreach (var exception in item.Exceptions)
                {
                    exception.Description = Markup(host, exception.Description, model);
                }
            }
        }

        private void BuildReference(IHostService host, ReferenceViewModel reference, FileModel model)
        {
            reference.Summary = Markup(host, reference.Summary, model);
        }

        private string Markup(IHostService host, string markdown, FileModel model)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return markdown;
            }
            var mr = host.Markup(markdown, model.FileAndType);
            ((HashSet<string>)model.Properties.LinkToFiles).UnionWith(mr.LinkToFiles);
            ((HashSet<string>)model.Properties.LinkToUids).UnionWith(mr.LinkToUids);
            return mr.Html;
        }

        #endregion
    }
}
