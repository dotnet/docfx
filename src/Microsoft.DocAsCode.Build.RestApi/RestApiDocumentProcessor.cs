// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.RestApi.Swagger;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(typeof(IDocumentProcessor))]
    public class RestApiDocumentProcessor : DisposableDocumentProcessor
    {
        private const string RestApiDocumentType = "RestApi";
        private const string DocumentTypeKey = "documentType";
        private static readonly string[] SupportedFileEndings = new string[]
        {
           "_swagger2.json",
           "_swagger.json",
           ".swagger.json",
           ".swagger2.json",
        };

        [ImportMany(nameof(RestApiDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override string Name => nameof(RestApiDocumentProcessor);

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    if (IsSupportedFile(file.File))
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
                    var filePath = Path.Combine(file.BaseDir, file.File);
                    var swaggerContent = File.ReadAllText(filePath);
                    var swagger = SwaggerJsonParser.Parse(swaggerContent);
                    swagger.Metadata[DocumentTypeKey] = RestApiDocumentType;
                    swagger.Raw = swaggerContent;
                    var repoInfo = GitUtility.GetGitDetail(filePath);
                    if (repoInfo != null)
                    {
                        swagger.Metadata["source"] = new SourceDetail() { Remote = repoInfo };
                    }

                    swagger.Metadata = MergeMetadata(swagger.Metadata, metadata);
                    var vm = SwaggerModelConverter.FromSwaggerModel(swagger);
                    var displayLocalPath = repoInfo?.RelativePath ?? filePath.ToDisplayPath();
                    return new FileModel(file, vm, serializer: new BinaryFormatter())
                    {
                        Uids = new[] { new UidDefinition(vm.Uid, displayLocalPath) }
                            .Concat(from item in vm.Children select new UidDefinition(item.Uid, displayLocalPath))
                            .Concat(from tag in vm.Tags select new UidDefinition(tag.Uid, displayLocalPath)).ToImmutableArray(),
                        LocalPathFromRepoRoot = displayLocalPath,
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
            var vm = (RestApiRootItemViewModel)model.Content;
            string documentType = null;
            object documentTypeObject;
            if (vm.Metadata.TryGetValue(DocumentTypeKey, out documentTypeObject))
            {
                documentType = documentTypeObject as string;
            }

            model.File = ChangeFileExtension(model.File);
            return new SaveResult
            {
                DocumentType = documentType ?? RestApiDocumentType,
                FileWithoutExtension = Path.ChangeExtension(model.File, null),
                LinkToFiles = model.LinkToFiles,
                LinkToUids = model.LinkToUids,
            };
        }

        #region Private methods

        private bool IsSupportedFile(string file)
        {
            return SupportedFileEndings.Any(s => IsSupported(file, s));
        }

        private bool IsSupported(string file, string fileEnding)
        {
            return file.EndsWith(fileEnding, StringComparison.OrdinalIgnoreCase);
        }

        private string ChangeFileExtension(string file)
        {
            return file.Substring(0, file.Length - SupportedFileEndings.First(s => IsSupported(file, s)).Length) + ".json";
        }

        private static Dictionary<string, object> MergeMetadata(IDictionary<string, object> item, IDictionary<string, object> overwriteItems)
        {
            var result = new Dictionary<string, object>(item);
            foreach (var pair in overwriteItems)
            {
                if (result.ContainsKey(pair.Key))
                {
                    Logger.LogWarning($"Metadata \"{pair.Key}\" inside rest api is overwritten.");
                }

                result[pair.Key] = pair.Value;
            }
            return result;
        }

        #endregion
    }
}
