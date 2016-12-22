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
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    [Export(typeof(IDocumentProcessor))]
    public class ManagedReferenceDocumentProcessor
        : ReferenceDocumentProcessorBase, ISupportIncrementalDocumentProcessor
    {
        #region Fields
        private readonly ResourcePoolManager<JsonSerializer> _serializerPool;
        #endregion

        #region Constructors

        public ManagedReferenceDocumentProcessor()
        {
            _serializerPool = new ResourcePoolManager<JsonSerializer>(GetSerializer, 0x10);
        }

        #endregion

        #region ReferenceDocumentProcessorBase Members

        protected override string ProcessorDocumentType { get; } = "ManagedReference";

        protected override FileModel LoadArticle(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
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
                LocalPathFromRoot = displayLocalPath
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
                        var mime = YamlMime.ReadMime(Path.Combine(file.BaseDir, file.File));
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
                Environment.Is64BitProcess ? null : new BinaryFormatter(),
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

            // Fill in bookmarks if template doesn't generate them.
            // TODO: remove these
            if (apiModel.Type == MemberType.Namespace) return;
            model.Bookmarks[apiModel.Uid] = string.Empty; // Reference's first level bookmark should have no anchor
            apiModel.Children?.ForEach(c =>
            {
                model.Bookmarks[c.Uid] = c.Id;
                if (!string.IsNullOrEmpty(c.Overload?.Uid))
                {
                    model.Bookmarks[c.Overload.Uid] = c.Overload.Id;
                }
            });
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

        private static IEnumerable<XRefSpec> GetXRefInfo(ItemViewModel item, string key,
            List<DataContracts.Common.ReferenceViewModel> references)
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
            if (!string.IsNullOrEmpty(item.NameWithType))
            {
                result["nameWithType"] = item.NameWithType;
            }
            if (!string.IsNullOrEmpty(item.NameWithTypeForCSharp))
            {
                result["nameWithType.csharp"] = item.NameWithTypeForCSharp;
            }
            if (!string.IsNullOrEmpty(item.NameWithTypeForVB))
            {
                result["nameWithType.vb"] = item.NameWithTypeForVB;
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

        private static XRefSpec GetXRefInfo(DataContracts.Common.ReferenceViewModel item, string key)
        {
            var result = new XRefSpec
            {
                Uid = item.Uid,
                Name = item.Name,
                Href = key,
                CommentId = item.CommentId,
            };
            string name;
            if (item.NameInDevLangs.TryGetValue("csharp", out name))
            {
                result["name.csharp"] = name;
            }
            if (item.NameInDevLangs.TryGetValue("vb", out name))
            {
                result["name.vb"] = name;
            }
            if (!string.IsNullOrEmpty(item.FullName))
            {
                result["fullName"] = item.FullName;
            }
            if (item.FullNameInDevLangs.TryGetValue("csharp", out name))
            {
                result["fullName.csharp"] = name;
            }
            if (item.FullNameInDevLangs.TryGetValue("vb", out name))
            {
                result["fullName.vb"] = name;
            }
            if (!string.IsNullOrEmpty(item.NameWithType))
            {
                result["nameWithType"] = item.NameWithType;
            }
            if (item.NameWithTypeInDevLangs.TryGetValue("csharp", out name))
            {
                result["nameWithType.csharp"] = name;
            }
            if (item.NameWithTypeInDevLangs.TryGetValue("vb", out name))
            {
                result["nameWithType.vb"] = name;
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
