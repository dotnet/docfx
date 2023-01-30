// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ConceptualDocuments
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    [Export(typeof(IDocumentProcessor))]
    public class ConceptualDocumentProcessor : DisposableDocumentProcessor
    {
        #region Fields

        private readonly ResourcePoolManager<JsonSerializer> _serializerPool;
        private readonly string[] SystemKeys = {
            "conceptual",
            "type",
            "source",
            "path",
            "documentation",
            "title",
            "rawTitle",
            "wordCount"
        };

        #endregion

        #region Constructors

        public ConceptualDocumentProcessor()
        {
            _serializerPool = new ResourcePoolManager<JsonSerializer>(GetSerializer, 0x10);
        }

        #endregion

        #region IDocumentProcessor Members

        [ImportMany(nameof(ConceptualDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override string Name => nameof(ConceptualDocumentProcessor);

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type != DocumentType.Article)
            {
                return ProcessingPriority.NotSupported;
            }
            if (".md".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
            {
                var filenameWithoutExtension = Path.ChangeExtension(file.File, null);

                // exclude overwrite markdown segments
                var subExtension = Path.GetExtension(filenameWithoutExtension);
                if ((".yml".Equals(subExtension, StringComparison.OrdinalIgnoreCase) ||
                    ".yaml".Equals(subExtension, StringComparison.OrdinalIgnoreCase))
                    && EnvironmentContext.FileAbstractLayer.Exists(filenameWithoutExtension))
                {
                    return ProcessingPriority.NotSupported;
                }

                return ProcessingPriority.Normal;
            }
            return ProcessingPriority.NotSupported;
        }

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            if (file.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }
            var content = MarkdownReader.ReadMarkdownAsConceptual(file.File);
            foreach (var (key, value) in metadata.OrderBy(item => item.Key))
            {
                content[key] = value;
            }
            content[Constants.PropertyName.SystemKeys] = SystemKeys;

            var localPathFromRoot = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file.File));

            return new FileModel(file, content)
            {
                LocalPathFromRoot = localPathFromRoot,
            };
        }

        public override SaveResult Save(FileModel model)
        {
            if (model.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }

            var result = new SaveResult
            {
                DocumentType = model.DocumentType ?? "Conceptual",
                FileWithoutExtension = Path.ChangeExtension(model.File, null),
                LinkToFiles = model.LinkToFiles.ToImmutableArray(),
                LinkToUids = model.LinkToUids,
                FileLinkSources = model.FileLinkSources,
                UidLinkSources = model.UidLinkSources,
            };
            if (model.Properties.XrefSpec != null)
            {
                result.XRefSpecs = ImmutableArray.Create(model.Properties.XrefSpec);
            }

            return result;
        }

        #endregion

        #region Protected Methods

        protected virtual void SerializeModel(object model, Stream stream)
        {
            using var sw = new StreamWriter(stream, Encoding.UTF8, 0x100, true);
            using var lease = _serializerPool.Rent();
            lease.Resource.Serialize(sw, model);
        }

        protected virtual object DeserializeModel(Stream stream)
        {
            using var sr = new StreamReader(stream, Encoding.UTF8, false, 0x100, true);
            using var jr = new JsonTextReader(sr);
            using var lease = _serializerPool.Rent();
            return lease.Resource.Deserialize(jr);
        }

        protected virtual void SerializeProperties(IDictionary<string, object> properties, Stream stream)
        {
            using var sw = new StreamWriter(stream, Encoding.UTF8, 0x100, true);
            using var lease = _serializerPool.Rent();
            lease.Resource.Serialize(sw, properties);
        }

        protected virtual IDictionary<string, object> DeserializeProperties(Stream stream)
        {
            using var sr = new StreamReader(stream, Encoding.UTF8, false, 0x100, true);
            using var jr = new JsonTextReader(sr);
            using var lease = _serializerPool.Rent();
            return lease.Resource.Deserialize<Dictionary<string, object>>(jr);
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
                TypeNameHandling = TypeNameHandling.All, // lgtm [cs/unsafe-type-name-handling]
            };
        }

        #endregion
    }
}
