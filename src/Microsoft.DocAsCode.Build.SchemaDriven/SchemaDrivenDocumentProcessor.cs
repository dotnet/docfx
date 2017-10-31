﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;

    using Newtonsoft.Json;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.SchemaDriven.Processors;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class SchemaDrivenDocumentProcessor
        : DisposableDocumentProcessor, ISupportIncrementalDocumentProcessor
    {
        #region Fields

        private readonly ResourcePoolManager<JsonSerializer> _serializerPool;
        private readonly string _schemaName;
        private readonly DocumentSchema _schema;
        private readonly SchemaValidator _schemaValidator;
        private readonly bool _allowOverwrite;
        #endregion

        public SchemaValidator SchemaValidator => _schemaValidator;
        #region Constructors

        public SchemaDrivenDocumentProcessor(DocumentSchema schema, ICompositionContainer container)
        {
            if (string.IsNullOrWhiteSpace(schema.Title))
            {
                throw new ArgumentException("Title for schema must not be empty");
            }

            _schemaName = schema.Title;
            _schema = schema;
            _schemaValidator = new SchemaValidator(_schema);
            _allowOverwrite = schema.AllowOverwrite;
            _serializerPool = new ResourcePoolManager<JsonSerializer>(GetSerializer, 0x10);
            if (container != null)
            {
                var commonSteps = container.GetExports<IDocumentBuildStep>(nameof(SchemaDrivenDocumentProcessor));
                var schemaSpecificSteps = container.GetExports<IDocumentBuildStep>($"{nameof(SchemaDrivenDocumentProcessor)}.{_schemaName}");
                BuildSteps = commonSteps.Union(schemaSpecificSteps).ToList();
            }
        }

        #endregion

        #region IDocumentProcessor Members

        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override string Name => _schema.Title;

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    if (".yml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase) ||
                        ".yaml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
                    {
                        var mime = YamlMime.ReadMime(file.File);
                        if (string.Equals(mime, YamlMime.YamlMimePrefix + _schemaName))
                        {
                            return ProcessingPriority.Normal;
                        }

                        return ProcessingPriority.NotSupported;
                    }

                    break;
                case DocumentType.Overwrite:
                    if (_allowOverwrite && ".md".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
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
                    // TODO: Support dynamic in YAML deserializer
                    try
                    {
                        // MUST be a dictionary
                        var obj = YamlUtility.Deserialize<Dictionary<string, object>>(file.File);

                        // Validate against the schema first
                        // Temporarily disable schema validation as Json.NET schema has limitation of 1000 calls per hour
                        // TODO: Reenable schema validation
                        // _schemaValidator.Validate(obj);

                        var content = ConvertToObjectHelper.ConvertToDynamic(obj);
                        var pageMetadata = _schema.MetadataReference.GetValue(content) as IDictionary<string, object>;
                        if (pageMetadata == null)
                        {
                            pageMetadata = new ExpandoObject();
                            _schema.MetadataReference.SetValue(ref content, pageMetadata);
                        }
                        foreach (var pair in metadata)
                        {
                            if (!pageMetadata.ContainsKey(pair.Key))
                            {
                                pageMetadata[pair.Key] = pair.Value;
                            }
                        }

                        var localPathFromRoot = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file.File));

                        var fm = new FileModel(
                            file,
                            content,
                            serializer: new BinaryFormatter())
                        {
                            LocalPathFromRoot = localPathFromRoot,
                        };

                        fm.Properties.Schema = _schema;
                        fm.Properties.Metadata = pageMetadata;
                        return fm;
                    }
                    catch (YamlDotNet.Core.YamlException e)
                    {
                        throw new DocumentException($"{file.File} is not in supported format: {e.Message}", e);
                    }
                case DocumentType.Overwrite:
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

            var result = new SaveResult
            {
                DocumentType = model.DocumentType ?? _schemaName,
                FileWithoutExtension = Path.ChangeExtension(model.File, null),
                LinkToFiles = model.LinkToFiles.ToImmutableArray(),
                LinkToUids = model.LinkToUids,
                FileLinkSources = model.FileLinkSources,
                UidLinkSources = model.UidLinkSources,
                XRefSpecs = ImmutableArray.CreateRange(model.Properties.XRefSpecs),
                ExternalXRefSpecs = ImmutableArray.CreateRange(model.Properties.ExternalXRefSpecs)
            };

            if (((IDictionary<string, object>)model.Properties).ContainsKey("XrefSpec"))
            {
                result.XRefSpecs = ImmutableArray.Create(model.Properties.XrefSpec);
            }

            return result;
        }

        public override void UpdateHref(FileModel model, IDocumentBuildContext context)
        {
            var content = model.Content;
            var pc = new ProcessContext(null, model, context);
            DocumentSchema schema = model.Properties.Schema;
            model.Content = new SchemaProcessor(
                new HrefInterpreter(false, true),
                new FileInterpreter(false, true)).Process(content, schema, pc);
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

        #region Protected Methods

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
                return lease.Resource.Deserialize<Dictionary<string, object>>(jr);
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
