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
    using System.Text;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    [Export(typeof(IDocumentProcessor))]
    public class ManagedReferenceDocumentProcessor
        : ReferenceDocumentProcessorBase, ISupportIncrementalDocumentProcessor
    {
        #region Fields
        private readonly ResourcePoolManager<JsonSerializer> _serializerPool;
        private static readonly string[] SystemKeys = {
            "uid",
            "isEii",
            "isExtensionMethod",
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
            "assemblies",
            "namespace",
            "summary",
            "remarks",
            "example",
            "syntax",
            "overridden",
            "overload",
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
            "attributes",
            Constants.PropertyName.AdditionalNotes
        };
        #endregion

        #region Constructors

        public ManagedReferenceDocumentProcessor()
        {
            _serializerPool = new ResourcePoolManager<JsonSerializer>(GetSerializer, 0x10);
        }

        #endregion

        #region ReferenceDocumentProcessorBase Members

        protected override string ProcessedDocumentType { get; } = "ManagedReference";

        protected override FileModel LoadArticle(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            if (YamlMime.ReadMime(file.File) == null)
            {
                Logger.LogWarning(
                    "Please add `YamlMime` as the first line of file, e.g.: `### YamlMime:ManagedReference`, otherwise the file will be not treated as ManagedReference source file in near future.",
                    file: file.File,
                    code: WarningCodes.Yaml.MissingYamlMime);
            }

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
                foreach (var item in metadata)
                {
                    if (!page.Metadata.ContainsKey(item.Key))
                    {
                        page.Metadata[item.Key] = item.Value;
                    }
                }
            }
            page.Metadata[Constants.PropertyName.SystemKeys] = SystemKeys;

            var localPathFromRoot = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file.File));

            return new FileModel(file, page, serializer: new BinaryFormatter())
            {
                Uids = (from item in page.Items select item.Uid)
                .Concat(from item in page.Items where item.Overload != null select item.Overload)
                .Distinct().Select(s => new UidDefinition(s, localPathFromRoot)).ToImmutableArray(),
                LocalPathFromRoot = localPathFromRoot
            };
        }

        #endregion

        #region IDocumentProcessor Members

        [ImportMany(nameof(ManagedReferenceDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override string Name => nameof(ManagedReferenceDocumentProcessor);

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
                            case YamlMime.ManagedReference:
                                return ProcessingPriority.Normal;
                            case null:
                                return ProcessingPriority.BelowNormal;
                            default:
                                return ProcessingPriority.NotSupported;
                        }
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
            UpdateModelContent(model);

            return result;
        }

        #endregion

        #region ISupportIncrementalDocumentProcessor Members

        public virtual string GetIncrementalContextHash()
        {
            return null;
        }

        public virtual void SaveIntermediateModel(FileModel model, Stream stream)
        {
            FileModelPropertySerialization.Serialize(
                model,
                stream,
                SerializeModel,
                SerializeProperties,
                null);
        }

        public virtual FileModel LoadIntermediateModel(Stream stream)
        {
            return FileModelPropertySerialization.Deserialize(
                stream,
                new BinaryFormatter(),
                DeserializeModel,
                DeserializeProperties,
                null);
        }

        #endregion

        #region Protected/Private Methods

        protected virtual void UpdateModelContent(FileModel model)
        {
            var apiModel = ApiBuildOutput.FromModel((PageViewModel)model.Content); // Fill in details
            model.Content = apiModel;
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

        private static IEnumerable<XRefSpec> GetXRefInfo(ItemViewModel item, string key,
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
                IsSpec = item.IsExternal != true,
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
                    if (pair.Value is string s)
                    {
                        result[pair.Key] = s;
                    }
                }
            }
            return result;
        }

        protected virtual void SerializeModel(object model, Stream stream)
        {
            using (var sw = new StreamWriter(stream, Encoding.UTF8, 0x100, true))
            using (var lease = _serializerPool.Rent())
            {
                lease.Resource.Serialize(sw, model);
            }
        }

        protected virtual object DeserializeModel(Stream stream)
        {
            using (var sr = new StreamReader(stream, Encoding.UTF8, false, 0x100, true))
            using (var jr = new JsonTextReader(sr))
            using (var lease = _serializerPool.Rent())
            {
                return lease.Resource.Deserialize(jr);
            }
        }

        protected virtual void SerializeProperties(IDictionary<string, object> properties, Stream stream)
        {
            using (var sw = new StreamWriter(stream, Encoding.UTF8, 0x100, true))
            using (var lease = _serializerPool.Rent())
            {
                lease.Resource.Serialize(sw, properties);
            }
        }

        protected virtual IDictionary<string, object> DeserializeProperties(Stream stream)
        {
            using (var sr = new StreamReader(stream, Encoding.UTF8, false, 0x100, true))
            using (var jr = new JsonTextReader(sr))
            using (var lease = _serializerPool.Rent())
            {
                return (IDictionary<string, object>)lease.Resource.Deserialize<object>(jr);
            }
        }

        protected virtual JsonSerializer GetSerializer()
        {
            return new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Converters =
                {
                    new Newtonsoft.Json.Converters.StringEnumConverter(),
                },
                TypeNameHandling = TypeNameHandling.All,
            };
        }

        #endregion
    }
}
