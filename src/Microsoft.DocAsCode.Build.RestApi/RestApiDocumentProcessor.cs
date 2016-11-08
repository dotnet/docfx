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
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [Export(typeof(IDocumentProcessor))]
    public class RestApiDocumentProcessor : DisposableDocumentProcessor
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
                    CheckOperationId(swagger, file.File);

                    var repoInfo = GitUtility.GetGitDetail(filePath);
                    if (repoInfo != null)
                    {
                        swagger.Metadata["source"] = new SourceDetail() { Remote = repoInfo };
                    }

                    swagger.Metadata = MergeMetadata(swagger.Metadata, metadata);
                    var vm = SwaggerModelConverter.FromSwaggerModel(swagger);
                    var displayLocalPath = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, file.FullPath);

                    return new FileModel(file, vm, serializer: Environment.Is64BitProcess ? null : new BinaryFormatter())
                    {
                        Uids = new[] { new UidDefinition(vm.Uid, displayLocalPath) }
                            .Concat(from item in vm.Children select new UidDefinition(item.Uid, displayLocalPath))
                            .Concat(from tag in vm.Tags select new UidDefinition(tag.Uid, displayLocalPath)).ToImmutableArray(),
                        LocalPathFromRepoRoot = repoInfo?.RelativePath ?? filePath.ToDisplayPath(),
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
                LinkToFiles = model.LinkToFiles.ToImmutableArray(),
                LinkToUids = model.LinkToUids,
                FileLinkSources = model.FileLinkSources,
                UidLinkSources = model.UidLinkSources,
                XRefSpecs = GetXRefInfo(vm, model.Key).ToImmutableArray()
            };
        }

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
                using (var streamReader = File.OpenText(filePath))
                using (JsonReader reader = new JsonTextReader(streamReader))
                {
                    var jObject = JObject.Load(reader);
                    JToken swaggerValue;
                    if (jObject.TryGetValue("swagger", out swaggerValue))
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
                        JToken operationId;
                        var jObject = operation.Value as JObject;
                        if (jObject != null && !jObject.TryGetValue(OperationIdKey, out operationId))
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
