// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text.RegularExpressions;
    using System.Threading;

    using Newtonsoft.Json;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.EntityModel.Swagger;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(typeof(IDocumentProcessor))]
    public class RestApiDocumentProcessor : DisposableDocumentProcessor
    {
        private const string RestApiDocumentType = "RestApi";
        private const string DocumentTypeKey = "documentType";
        /// <summary>
        /// TODO: resolve JSON reference $ref
        /// </summary>
        private static readonly ThreadLocal<JsonSerializer> _serializer = new ThreadLocal<JsonSerializer>(
            () =>
            {
                var jsonSerializer = new JsonSerializer();
                jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                jsonSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;

                // Newtonsoft JSON Reference object does not allow additional content together with $ref however swagger allows:
                // "schema": {
                //           "$ref": "#/definitions/contact",
                //           "example": {
                //               "department": "Sales",
                //               "jobTitle": "Sales Rep"
                //           }
                //       }
                jsonSerializer.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
                return jsonSerializer;
            });

        [ImportMany(nameof(RestApiDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override string Name => nameof(RestApiDocumentProcessor);

        /// <summary>
        /// TODO: override document
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    if (file.File.EndsWith("_swagger2.json", StringComparison.OrdinalIgnoreCase) ||
                        file.File.EndsWith("_swagger.json", StringComparison.OrdinalIgnoreCase) ||
                        file.File.EndsWith(".swagger.json", StringComparison.OrdinalIgnoreCase) ||
                        file.File.EndsWith(".swagger2.json", StringComparison.OrdinalIgnoreCase))
                    {
                        return ProcessingPriority.Normal;
                    }
                    break;
                default:
                    break;
            }
            return ProcessingPriority.NotSupportted;
        }

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            if (file.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }
            var filePath = Path.Combine(file.BaseDir, file.File);
            var swagger = GetModelWithoutRef<SwaggerModel>(filePath);
            swagger.Metadata[DocumentTypeKey] = RestApiDocumentType;
            var repoInfo = GitUtility.GetGitDetail(filePath);
            if (repoInfo != null)
            {
                swagger.Metadata["source"] = new SourceDetail() { Remote = repoInfo };
            }

            var uid = GetUid(swagger);
            var vm = new RestApiViewModel
            {
                Name = swagger.Info.Title,
                Uid = uid,
                HtmlId = GetHtmlId(uid),
                Metadata = MergeMetadata(swagger.Metadata, metadata),
                Description = swagger.Description,
                Summary = swagger.Summary
            };
            foreach (var path in swagger.Paths)
            {
                foreach (var op in path.Value)
                {
                    var itemUid = uid + "/" + op.Value.OperationId;
                    var itemVm = new RestApiItemViewModel
                    {
                        Path = path.Key,
                        OperationName = op.Key,
                        OperationId = op.Value.OperationId,
                        HtmlId = GetHtmlId(itemUid),
                        Uid = itemUid,
                        Metadata = op.Value.Metadata,
                        Description = op.Value.Description,
                        Summary = op.Value.Summary,
                        Parameters = op.Value.Parameters?.Select(s => new RestApiParameterViewModel
                        {
                            Description = s.Description,
                            Metadata = s.Metadata
                        }).ToList(),
                        Responses = op.Value.Responses?.Select(s => new RestApiResponseViewModel
                        {
                            Metadata = s.Value.Metadata,
                            Description = s.Value.Description,
                            Summary = s.Value.Summary,
                            HttpStatusCode = s.Key,
                            Examples = s.Value.Examples.Select(example => new RestApiResponseExampleViewModel
                            {
                                MimeType = example.Key,
                                Content = example.Value != null ? JsonUtility.Serialize(example.Value) : null,
                            }).ToList(),
                        }).ToList(),
                    };

                    // TODO: line number
                    itemVm.Metadata["source"] = swagger.Metadata["source"];
                    vm.Children.Add(itemVm);
                }
            }
            var displayLocalPath = repoInfo?.RelativePath ?? Path.Combine(file.BaseDir, file.File).ToDisplayPath();
            return new FileModel(file, vm, serializer: new BinaryFormatter())
            {
                Uids = new UidDefinition[] { new UidDefinition(vm.Uid, displayLocalPath) }.Concat(from item in vm.Children select new UidDefinition(item.Uid, displayLocalPath)).ToImmutableArray(),
                LocalPathFromRepoRoot = displayLocalPath,
                Properties =
                {
                    LinkToFiles = new HashSet<string>(),
                    LinkToUids = new HashSet<string>(),
                },
            };
        }

        public override SaveResult Save(FileModel model)
        {
            if (model.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }
            var vm = (RestApiViewModel)model.Content;
            string documentType = null;
            object documentTypeObject;
            if (vm.Metadata.TryGetValue(DocumentTypeKey, out documentTypeObject))
            {
                documentType = documentTypeObject as string;
            }
            return new SaveResult
            {
                DocumentType = documentType ?? RestApiDocumentType,
                ModelFile = model.File,
                LinkToFiles = ((HashSet<string>)model.Properties.LinkToFiles).ToImmutableArray(),
                LinkToUids = ((HashSet<string>)model.Properties.LinkToUids).ToImmutableHashSet(),
            };
        }

        internal static T GetModelWithoutRef<T>(string path)
        {
            return JsonUtility.Deserialize<T>(path, _serializer.Value);
        }

        #region Private methods
        private static Regex HtmlEncodeRegex = new Regex(@"\W", RegexOptions.Compiled);

        /// <summary>
        /// TODO: merge with the one in XrefDetails
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static string GetHtmlId(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return HtmlEncodeRegex.Replace(id, "_");
        }

        private static Dictionary<string, object> MergeMetadata(IDictionary<string, object> item, IDictionary<string, object> overrideItem)
        {
            var result = new Dictionary<string, object>(item);
            foreach (var pair in overrideItem)
            {
                if (result.ContainsKey(pair.Key))
                {
                    Logger.LogWarning($"Metadata \"{pair.Key}\" inside rest api is overridden.");
                }

                result[pair.Key] = pair.Value;
            }
            return result;
        }

        private static string GetUid(SwaggerModel swagger)
        {
            var uid = string.Empty;
            if (!string.IsNullOrEmpty(swagger.Host))
            {
                uid += swagger.Host + "/";
            }
            if (!string.IsNullOrEmpty(swagger.BasePath))
            {
                uid += swagger.BasePath + "/";
            }
            uid += swagger.Info.Title;
            return uid;
        }

        #endregion
    }
}
