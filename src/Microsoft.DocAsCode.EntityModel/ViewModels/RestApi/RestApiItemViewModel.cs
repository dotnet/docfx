// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility.EntityMergers;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class RestApiItemViewModel : IOverwriteDocumentViewModel
    {
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        [MergeOption(MergeOption.MergeKey)]
        public string Uid { get; set; }

        [YamlMember(Alias = "htmlId")]
        [JsonProperty("htmlId")]
        [MergeOption(MergeOption.Ignore)]
        public string HtmlId { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Path)]
        [JsonProperty(Constants.PropertyName.Path)]
        public string Path { get; set; }

        [YamlMember(Alias = "operation")]
        [JsonProperty("operation")]
        public string OperationName { get; set; }

        [YamlMember(Alias = "operationId")]
        [JsonProperty("operationId")]
        public string OperationId { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        /// <summary>
        /// The original swagger.json cpntent
        /// `_` prefix indicates that this metadata is generated
        /// </summary>
        [YamlMember(Alias = "_raw")]
        [JsonProperty("_raw")]
        [MergeOption(MergeOption.Ignore)]
        public string Raw { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Documentation)]
        [JsonProperty(Constants.PropertyName.Documentation)]
        public SourceDetail Documentation { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Conceptual)]
        [JsonProperty(Constants.PropertyName.Conceptual)]
        public string Conceptual { get; set; }

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<RestApiParameterViewModel> Parameters { get; set; }

        [YamlMember(Alias = "responses")]
        [JsonProperty("responses")]
        public List<RestApiResponseViewModel> Responses { get; set; }

        [YamlMember(Alias = "children")]
        [JsonProperty("children")]
        public List<RestApiItemViewModel> Children { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public static RestApiItemViewModel FromSwaggerModel(Swagger.SwaggerModel swagger)
        {
            var uid = GetUid(swagger);
            var vm = new RestApiItemViewModel
            {
                Name = swagger.Info.Title,
                Uid = uid,
                HtmlId = GetHtmlId(uid),
                Metadata = swagger.Metadata,
                Description = swagger.Description,
                Summary = swagger.Summary,
                Children = new List<RestApiItemViewModel>(),
                Raw = swagger.Raw
            };
            foreach (var path in swagger.Paths)
            {
                foreach (var op in path.Value)
                {
                    var itemUid = GetUidForOperation(uid, op.Value);
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
                            Examples = s.Value.Examples?.Select(example => new RestApiResponseExampleViewModel
                            {
                                MimeType = example.Key,
                                Content = example.Value != null ? JsonUtility.Serialize(example.Value) : null,
                            }).ToList(),
                        }).ToList(),
                    };

                    // TODO: line number
                    itemVm.Metadata[Constants.PropertyName.Source] = swagger.Metadata[Constants.PropertyName.Source];
                    vm.Children.Add(itemVm);
                }
            }

            return vm;
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

        private static string GetUid(Swagger.SwaggerModel swagger)
        {
            return GenerateUid(swagger.Host, swagger.BasePath, swagger.Info.Title);
        }

        private static string GetUidForOperation(string parentUid, Swagger.OperationObject item)
        {
            return GenerateUid(parentUid, item.OperationId);
        }

        /// <summary>
        /// UID is joined by '/', if segment ends with '/', use that one instead
        /// </summary>
        /// <param name="segments">The segments to generate UID</param>
        /// <returns></returns>
        private static string GenerateUid(params string[] segments)
        {
            return string.Join("/", segments.Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim('/')));
        }

        #endregion
    }
}
