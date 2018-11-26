// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.UniversalReference;
    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IDocumentProcessor))]
    public class UniversalReferenceDocumentProcessor : ReferenceDocumentProcessorBase
    {
        #region ReferenceDocumentProcessorBase Members

        protected override string ProcessedDocumentType { get; } = UniversalReferenceConstants.UniversalReference;

        protected override FileModel LoadArticle(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            var page = YamlUtility.Deserialize<PageViewModel>(file.File);
            if (page.Items == null || page.Items.Count == 0)
            {
                Logger.LogWarning("No items found from YAML file. No output is generated");
                return null;
            }
            if (page.Items[0].SupportedLanguages == null || page.Items[0].SupportedLanguages.Length == 0)
            {
                throw new ArgumentException($"{nameof(ItemViewModel.SupportedLanguages)} must contain at least one language");
            }
            if (page.Items.Any(i => string.IsNullOrEmpty(i.Uid)))
            {
                throw new ArgumentException($"{nameof(ItemViewModel.Uid)} must not be null or empty");
            }
            if (page.Metadata == null)
            {
                page.Metadata = metadata.ToDictionary(p => p.Key, p => p.Value);
            }
            else
            {
                foreach (var item in metadata.Where(item => !page.Metadata.ContainsKey(item.Key)))
                {
                    page.Metadata[item.Key] = item.Value;
                }
            }

            var localPathFromRoot = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file.File));

            return new FileModel(file, page, serializer: new BinaryFormatter())
            {
                Uids = (from item in page.Items select new UidDefinition(item.Uid, localPathFromRoot)).ToImmutableArray(),
                LocalPathFromRoot = localPathFromRoot
            };
        }

        #endregion

        #region IDocumentProcessor Members

        [ImportMany(nameof(UniversalReferenceDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override string Name => nameof(UniversalReferenceDocumentProcessor);

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    if (".yml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase) ||
                        ".yaml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
                    {
                        var mime = YamlMime.ReadMime(file.File);
                        switch (mime)
                        {
                            case UniversalReferenceConstants.UniversalReferenceYamlMime:
                                return ProcessingPriority.Normal;
                            default:
                                return ProcessingPriority.NotSupported;
                        }
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

        public override SaveResult Save(FileModel model)
        {
            var vm = (PageViewModel)model.Content;

            var result = base.Save(model);
            result.XRefSpecs = (from item in vm.Items
                                from xref in GetXRefInfo(item, model.Key, vm.References)
                                group xref by xref.Uid
                                into g
                                select g.First()).ToImmutableArray();
            result.ExternalXRefSpecs = GetXRefFromReference(vm).ToImmutableArray();
            try
            {
                UpdateModelContent(model);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
                throw;
            }

            return result;
        }

        #endregion

        #region Protected/Private Methods

        protected virtual void UpdateModelContent(FileModel model)
        {
            model.Content = ModelConverter.ToApiBuildOutput((PageViewModel)model.Content);
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
                    yield return GetXRefSpecFromReference(reference);
                }
            }
        }

        private static IEnumerable<XRefSpec> GetXRefInfo(
            ItemViewModel item,
            string key,
            List<ReferenceViewModel> references)
        {
            var result = new XRefSpec
            {
                Uid = item.Uid,
                Name = item.Name,
                Href = ((RelativePath)key).UrlEncode().ToString(),
                CommentId = item.CommentId,
            };
            if (item.Names.Count > 0)
            {
                foreach (var pair in item.Names)
                {
                    result["name." + pair.Key] = pair.Value;
                }
            }
            if (!string.IsNullOrEmpty(item.FullName))
            {
                result["fullName"] = item.FullName;
            }
            if (item.FullNames.Count > 0)
            {
                foreach (var pair in item.FullNames)
                {
                    result["fullName." + pair.Key] = pair.Value;
                }
            }
            if (!string.IsNullOrEmpty(item.NameWithType))
            {
                result["nameWithType"] = item.NameWithType;
            }
            if (item.NamesWithType.Count > 0)
            {
                foreach (var pair in item.NamesWithType)
                {
                    result["nameWithType." + pair.Key] = pair.Value;
                }
            }
            yield return result;

            // generate overload xref spec.
            if (item.Overload != null)
            {
                var reference = references.Find(r => r.Uid == item.Overload);
                if (reference != null)
                {
                    yield return GetXRefInfo(reference, key);
                }
            }
        }

        private static XRefSpec GetXRefInfo(ReferenceViewModel item, string key)
        {
            var result = GetXRefSpecFromReference(item);
            result.Href = ((RelativePath)key).UrlEncode().ToString();
            return result;
        }

        private static XRefSpec GetXRefSpecFromReference(ReferenceViewModel item)
        {
            var result = new XRefSpec
            {
                Uid = item.Uid,
                Name = item.Name,
                Href = item.Href,
                CommentId = item.CommentId,
            };
            if (item.NameInDevLangs.Count > 0)
            {
                foreach (var pair in item.NameInDevLangs)
                {
                    result["name." + pair.Key] = pair.Value;
                }
            }
            if (!string.IsNullOrEmpty(item.FullName))
            {
                result["fullName"] = item.FullName;
            }
            if (item.FullNameInDevLangs.Count > 0)
            {
                foreach (var pair in item.FullNameInDevLangs)
                {
                    result["fullName." + pair.Key] = pair.Value;
                }
            }
            if (!string.IsNullOrEmpty(item.NameWithType))
            {
                result["nameWithType"] = item.NameWithType;
            }
            if (item.NameWithTypeInDevLangs.Count > 0)
            {
                foreach (var pair in item.NameWithTypeInDevLangs)
                {
                    result["nameWithType." + pair.Key] = pair.Value;
                }
            }
            if (item.Additional != null)
            {
                foreach (var pair in item.Additional)
                {
                    var s = pair.Value as string;
                    if (s != null)
                    {
                        result[pair.Key] = s;
                    }
                }
            }
            return result;
        }

        #endregion
    }
}
