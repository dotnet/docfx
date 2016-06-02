// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Utility.EntityMergers;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class RestApiRootItemViewModel : RestApiItemViewModelBase
    {
        private const string TagText = "tag";

        /// <summary>
        /// The original swagger.json cpntent
        /// `_` prefix indicates that this metadata is generated
        /// </summary>
        [YamlMember(Alias = "_raw")]
        [JsonProperty("_raw")]
        [MergeOption(MergeOption.Ignore)]
        public string Raw { get; set; }

        [YamlMember(Alias = "tags")]
        [JsonProperty("tags")]
        public List<RestApiTagViewModel> Tags { get; set; }

        [YamlMember(Alias = "children")]
        [JsonProperty("children")]
        public List<RestApiChildItemViewModel> Children { get; set; }

        public static RestApiRootItemViewModel FromSwaggerModel(Swagger.SwaggerModel swagger)
        {
            var uid = GetUid(swagger);
            var vm = new RestApiRootItemViewModel
            {
                Name = swagger.Info.Title,
                Uid = uid,
                HtmlId = GetHtmlId(uid),
                Metadata = swagger.Metadata,
                Description = swagger.Description,
                Summary = swagger.Summary,
                Children = new List<RestApiChildItemViewModel>(),
                Raw = swagger.Raw,
                Tags = new List<RestApiTagViewModel>()
            };
            if (swagger.Tags != null)
            {
                foreach (var tag in swagger.Tags)
                {
                    vm.Tags.Add(new RestApiTagViewModel
                    {
                        Name = tag.Name,
                        Description = tag.Description,
                        HtmlId = string.IsNullOrEmpty(tag.BookmarkId) ? GetHtmlId(tag.Name) : tag.BookmarkId, // Fall back to tag name's html id
                        Metadata = tag.Metadata,
                        Uid = GetUidForTag(uid, tag)
                    });
                }
            }
            foreach (var path in swagger.Paths)
            {
                foreach (var op in path.Value)
                {
                    var itemUid = GetUidForOperation(uid, op.Value);
                    var itemVm = new RestApiChildItemViewModel
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

        private static readonly Regex HtmlEncodeRegex = new Regex(@"\W", RegexOptions.Compiled);

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
            return GenerateUid(swagger.Host, swagger.BasePath, swagger.Info.Title, swagger.Info.Version);
        }

        private static string GetUidForOperation(string parentUid, Swagger.OperationObject item)
        {
            return GenerateUid(parentUid, item.OperationId);
        }

        private static string GetUidForTag(string parentUid, Swagger.TagItemObject tag)
        {
            return GenerateUid(parentUid, TagText, tag.Name);
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
