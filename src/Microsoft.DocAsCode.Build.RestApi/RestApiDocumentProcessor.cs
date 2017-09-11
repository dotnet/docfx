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
    using Microsoft.DocAsCode.Common.Git;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [Export(typeof(IDocumentProcessor))]
    public class RestApiDocumentProcessor : ReferenceDocumentProcessorBase
    {
        private const string RestApiDocumentType = "RestApi";
        private const string DocumentTypeKey = "documentType";
        private const string OperationIdKey = "operationId";

        // To keep backward compatibility, still support and change previous file endings by first mapping sequence.
        // Take 'a.b_swagger2.json' for an example, the json file name would be changed to 'a.b', then the html file name would be 'a.b.html'.
        private static readonly string[] SupportedFileEndings =
        {
           "_swagger2.json",
           "_swagger.json",
           ".swagger.json",
           ".swagger2.json",
           ".json",
        };

        protected static readonly string[] SystemKeys = {
            "uid",
            "htmlId",
            "name",
            "conceptual",
            "description",
            "remarks",
            "summary",
            "documentation",
            "children",
            "documentType",
            "source",
            // Swagger Object Fields (http://swagger.io/specification/#schema-13):
            "swagger",
            "info",
            "host",
            "basePath",
            "schemes",
            "consumes",
            "produces",
            "paths",
            "definitions",
            "parameters",
            "responses",
            "securityDefinitions",
            "security",
            "tags",
            "externalDocs"
        };

        [ImportMany(nameof(RestApiDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override string Name => nameof(RestApiDocumentProcessor);

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    if (IsSupportedFile(file.FullPath))
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
            if (model.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }
            var vm = (RestApiRootItemViewModel)model.Content;

            if (vm.Metadata.TryGetValue(DocumentTypeKey, out object documentTypeObject))
            {
                if (documentTypeObject is string documentType)
                {
                    model.DocumentType = documentType;
                }
            }
            model.File = ChangeFileExtension(model.File);

            var result = base.Save(model);
            result.XRefSpecs = GetXRefInfo(vm, model.Key).ToImmutableArray();
            return result;
        }

        #region ReferenceDocumentProcessorBase Members

        protected override string ProcessedDocumentType { get; } = RestApiDocumentType;

        protected override FileModel LoadArticle(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            var filePath = Path.Combine(file.BaseDir, file.File);
            var swagger = SwaggerJsonParser.Parse(filePath);
            swagger.Metadata[DocumentTypeKey] = RestApiDocumentType;
            swagger.Raw = EnvironmentContext.FileAbstractLayer.ReadAllText(filePath);
            CheckOperationId(swagger, file.File);

            var repoInfo = GitUtility.TryGetFileDetail(filePath);
            if (repoInfo != null)
            {
                swagger.Metadata["source"] = new SourceDetail() { Remote = repoInfo };
            }

            swagger.Metadata = MergeMetadata(swagger.Metadata, metadata);
            var vm = SwaggerModelConverter.FromSwaggerModel(swagger);
            vm.Metadata[Constants.PropertyName.SystemKeys] = SystemKeys;
            var displayLocalPath = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, file.FullPath);

            return new FileModel(file, vm, serializer: new BinaryFormatter())
            {
                Uids = new[] { new UidDefinition(vm.Uid, displayLocalPath) }
                    .Concat(from item in vm.Children
                            where !string.IsNullOrEmpty(item.Uid)
                            select new UidDefinition(item.Uid, displayLocalPath))
                    .Concat(from tag in vm.Tags
                            where !string.IsNullOrEmpty(tag.Uid)
                            select new UidDefinition(tag.Uid, displayLocalPath)).ToImmutableArray(),
                LocalPathFromRoot = displayLocalPath
            };
        }

        #endregion

        #region Private methods

        private static IEnumerable<XRefSpec> GetXRefInfo(RestApiRootItemViewModel rootItem, string key)
        {
            yield return new XRefSpec
            {
                Uid = rootItem.Uid,
                Name = rootItem.Name,
                Href = key,
            };

            if (rootItem.Children != null)
            {
                foreach (var child in rootItem.Children)
                {
                    yield return new XRefSpec
                    {
                        Uid = child.Uid,
                        Name = child.OperationId,
                        Href = key,
                    };
                }
            }

            if (rootItem.Tags != null)
            {
                foreach (var tag in rootItem.Tags)
                {
                    yield return new XRefSpec
                    {
                        Uid = tag.Uid,
                        Name = tag.Name,
                        Href = key,
                    };
                }
            }
        }

        private static bool IsSupportedFile(string filePath)
        {
            return SupportedFileEndings.Any(s => IsSupportedFileEnding(filePath, s)) && IsSwaggerFile(filePath);
        }

        private static bool IsSupportedFileEnding(string filePath, string fileEnding)
        {
            return filePath.EndsWith(fileEnding, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSwaggerFile(string filePath)
        {
            try
            {
                using (var streamReader = EnvironmentContext.FileAbstractLayer.OpenReadText(filePath))
                using (JsonReader reader = new JsonTextReader(streamReader))
                {
                    var jObject = JObject.Load(reader);
                    if (jObject.TryGetValue("swagger", out JToken swaggerValue))
                    {
                        var swaggerString = (string)swaggerValue;
                        if (swaggerString != null && swaggerString.Equals("2.0"))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Logger.LogVerbose($"In {nameof(RestApiDocumentProcessor)}, could not find {filePath}, exception details: {ex.Message}.");
            }
            catch (JsonException ex)
            {
                Logger.LogVerbose($"In {nameof(RestApiDocumentProcessor)}, could not deserialize {filePath} to JObject, exception details: {ex.Message}.");
            }

            return false;
        }

        private static void CheckOperationId(SwaggerModel swagger, string fileName)
        {
            if (swagger.Paths != null)
            {
                foreach (var path in swagger.Paths)
                {
                    foreach (var operation in path.Value.Metadata)
                    {
                        if (operation.Value is JObject jObject && !jObject.TryGetValue(OperationIdKey, out JToken operationId))
                        {
                            throw new DocfxException($"{OperationIdKey} should exist in operation '{operation.Key}' of path '{path.Key}' for swagger file '{fileName}'");
                        }
                    }
                }
            }
        }

        private static string ChangeFileExtension(string file)
        {
            return file.Substring(0, file.Length - SupportedFileEndings.First(s => IsSupportedFileEnding(file, s)).Length) + ".json";
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
