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

                    var displayLocalPath = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, file.FullPath);

                    return new FileModel(file, page, serializer: Environment.Is64BitProcess ? null : new BinaryFormatter())
                    {
                        Uids = (from item in page.Items select new UidDefinition(item.Uid, displayLocalPath)).ToImmutableArray(),
                        LocalPathFromRepoRoot = displayLocalPath,
                        LocalPathFromRoot = displayLocalPath
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

            UpdateModelContent(model);

            return new SaveResult
            {
                DocumentType = "ManagedReference",
                FileWithoutExtension = Path.ChangeExtension(model.File, null),
                LinkToFiles = model.LinkToFiles.ToImmutableArray(),
                LinkToUids = model.LinkToUids,
                XRefSpecs = (from item in vm.Items
                             select GetXRefInfo(item, model.Key)).ToImmutableArray(),
                ExternalXRefSpecs = GetXRefFromReference(vm).ToImmutableArray(),
            };
        }

        protected virtual void UpdateModelContent(FileModel model)
        {
            model.Content = ApiBuildOutput.FromModel((PageViewModel)model.Content); // Fill in details
        }

        private IEnumerable<XRefSpec> GetXRefFromReference(PageViewModel vm)
        {
            if (vm.References == null)
            {
                yield break;
            }
            foreach (var reference in vm.References)
            {
                if (reference != null && reference.IsExternal != false)
                {
                    var dict = YamlUtility.ConvertTo<Dictionary<string, object>>(reference);
                    if (dict != null)
                    {
                        var spec = new XRefSpec();
                        foreach (var pair in dict)
                        {
                            var s = pair.Value as string;
                            if (s != null)
                            {
                                spec[pair.Key] = s;
                            }
                        }
                        yield return spec;
                    }
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
                CommentId = item.CommentId,
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
