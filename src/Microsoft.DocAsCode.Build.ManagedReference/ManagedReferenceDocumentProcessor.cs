﻿// Copyright (c) Microsoft. All rights reserved.
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
    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;

    [Export(typeof(IDocumentProcessor))]
    public class ManagedReferenceDocumentProcessor
        : DisposableDocumentProcessor, ISupportIncrementalDocumentProcessor
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
                FileLinkSources = model.FileLinkSources,
                UidLinkSources = model.UidLinkSources,
                XRefSpecs = (from item in vm.Items
                             from xref in GetXRefInfo(item, model.Key)
                             select xref).ToImmutableArray(),
                ExternalXRefSpecs = GetXRefFromReference(vm).ToImmutableArray(),
            };
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

        private static IEnumerable<XRefSpec> GetXRefInfo(ItemViewModel item, string key)
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
            yield return result;
            // generate overload xref spec.
            // todo : remove when overload is ready in yaml file.
            if (item.Type != null)
            {
                switch (item.Type.Value)
                {
                    case MemberType.Property:
                    case MemberType.Constructor:
                    case MemberType.Method:
                    case MemberType.Operator:
                        yield return GenerateOverloadXrefSpec(item, key);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Work around, remove when overload is ready in yaml file.
        /// </summary>
        private static XRefSpec GenerateOverloadXrefSpec(ItemViewModel item, string key)
        {
            var uidBody = item.Uid;
            {
                var index = uidBody.IndexOf('(');
                if (index != -1)
                {
                    uidBody = uidBody.Remove(index);
                }
            }
            uidBody = System.Text.RegularExpressions.Regex.Replace(uidBody, @"``\d+$", string.Empty);
            var result = new XRefSpec
            {
                Uid = uidBody + "*",
                Href = key,
                CommentId = "Overload:" + uidBody,
            };
            {
                var index = item.Name.IndexOfAny(new char[] { '(', '[' });
                if (index != -1)
                {
                    result.Name = item.Name.Remove(index);
                }
                else
                {
                    result.Name = item.Name;
                }
            }
            if (!string.IsNullOrEmpty(item.NameForCSharp))
            {
                var index = item.NameForCSharp.IndexOfAny(new char[] { '(', '[' });
                if (index != -1)
                {
                    result["name.csharp"] = item.NameForCSharp.Remove(index);
                }
                else
                {
                    result["name.csharp"] = item.NameForCSharp;
                }
            }
            if (!string.IsNullOrEmpty(item.NameForVB))
            {
                var index = item.NameForVB.IndexOfAny(new char[] { '(', '[' });
                if (index != -1)
                {
                    result["name.vb"] = item.NameForVB.Remove(index);
                }
                else
                {
                    result["name.vb"] = item.NameForVB;
                }
            }
            if (!string.IsNullOrEmpty(item.FullName))
            {
                var index = item.FullName.IndexOfAny(new char[] { '(', '[' });
                if (index != -1)
                {
                    result["fullName"] = item.FullName.Remove(index);
                }
                else
                {
                    result["fullName"] = item.FullName;
                }
                result["fullName"] = item.FullName;
            }
            if (!string.IsNullOrEmpty(item.FullNameForCSharp))
            {
                var index = item.FullNameForCSharp.IndexOfAny(new char[] { '(', '[' });
                if (index != -1)
                {
                    result["fullName.csharp"] = item.FullNameForCSharp.Remove(index);
                }
                else
                {
                    result["fullName.csharp"] = item.FullNameForCSharp;
                }
            }
            if (!string.IsNullOrEmpty(item.FullNameForVB))
            {
                var index = item.FullNameForVB.IndexOfAny(new char[] { '(', '[' });
                if (index != -1)
                {
                    result["fullName.vb"] = item.FullNameForVB.Remove(index);
                }
                else
                {
                    result["fullName.vb"] = item.FullNameForVB;
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
