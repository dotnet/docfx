// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
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
    using DataContracts = Microsoft.DocAsCode.DataContracts.Common ;
    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IDocumentProcessor))]
    public class JavaScriptReferenceDocumentProcessor : ReferenceDocumentProcessorBase
    {
        private static readonly string[] SystemKeys = {
            "uid",
            "parent",
            "children",
            "href",
            "langs",
            "name",
            "nameWithType",
            "fullName",
            "type",
            "source",
            "documentation",
            "packages",
            "namespace",
            "summary",
            "remarks",
            "exmaple",
            "syntax",
            "overridden",
            "exceptions",
            "seealso",
            "see",
            "inheritance",
            "derivedClasses",
            "level",
            "implements",
            "inheritedMembers",
            "extensionMethods",
            "conceptual",
            "platform",
        };

        #region ReferenceDocumentProcessorBase Members

        protected override string ProcessedDocumentType => Constants.JavaScriptReferenceName;

        protected override FileModel LoadArticle(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            var page = YamlUtility.Deserialize<PageViewModel>(file.File);
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
                foreach (var item in metadata.Where(item => !page.Metadata.ContainsKey(item.Key)))
                {
                    page.Metadata[item.Key] = item.Value;
                }
            }
            page.Metadata[DataContracts.Constants.PropertyName.SystemKeys] = SystemKeys;

            var localPathFromRoot = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file.File));

            return new FileModel(file, page, serializer: new BinaryFormatter())
            {
                Uids = (from item
                        in page.Items
                        where !string.IsNullOrEmpty(item.Uid)
                        select new UidDefinition(item.Uid, localPathFromRoot)).ToImmutableArray(),
                LocalPathFromRoot = localPathFromRoot
            };
        }

        #endregion

        #region IDocumentProcessor Members

        [ImportMany(nameof(JavaScriptReferenceDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override string Name => nameof(JavaScriptReferenceDocumentProcessor);

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
                            case Constants.JavaScriptReferenceYamlMime:
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

            model.Content = ((PageViewModel) model.Content).ToApiBuildOutput();

            return result;
        }

        #endregion

        #region Protected/Private Methods

        private static IEnumerable<XRefSpec> GetXRefFromReference(PageViewModel vm)
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

        private static IEnumerable<XRefSpec> GetXRefInfo(ItemViewModel item, string key,
            List<DataContracts.ReferenceViewModel> references)
        {
            var result = new XRefSpec
            {
                Uid = item.Uid,
                Name = item.Name,
                Href = key,
            };
            if (!string.IsNullOrEmpty(item.FullName))
            {
                result["fullName"] = item.FullName;
            }
            if (!string.IsNullOrEmpty(item.NameWithType))
            {
                result["nameWithType"] = item.NameWithType;
            }
            yield return result;
        }


        #endregion
    }
}
