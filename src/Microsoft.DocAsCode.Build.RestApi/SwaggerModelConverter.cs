// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Build.RestApi.Swagger;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.RestApi;

    using Newtonsoft.Json.Linq;

    public static class SwaggerModelConverter
    {
        public static RestApiRootItemViewModel FromSwaggerModel(SwaggerModel swagger)
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
            if (swagger.Paths != null)
            {
                foreach (var path in swagger.Paths)
                {
                    var commonParameters = path.Value.Parameters;
                    foreach (var op in path.Value.Metadata)
                    {
                        // fetch operations from metadata
                        if (OperationNames.Contains(op.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            var opJObject = op.Value as JObject;
                            if (opJObject == null)
                            {
                                throw new InvalidOperationException($"Value of {op.Key} should be JObject");
                            }

                            // convert operation from JObject to OperationObject
                            var operation = opJObject.ToObject<OperationObject>();
                            var parameters = GetParametersForOperation(operation.Parameters, commonParameters);
                            var itemUid = GetUidForOperation(uid, operation);
                            var itemVm = new RestApiChildItemViewModel
                            {
                                Path = path.Key,
                                OperationName = op.Key,
                                Tags = operation.Tags,
                                OperationId = operation.OperationId,
                                HtmlId = GetHtmlId(itemUid),
                                Uid = itemUid,
                                Metadata = operation.Metadata,
                                Description = operation.Description,
                                Summary = operation.Summary,
                                Parameters = parameters?.Select(s => new RestApiParameterViewModel
                                {
                                    Description = s.Description,
                                    Name = s.Name,
                                    Metadata = s.Metadata
                                }).ToList(),
                                Responses = operation.Responses?.Select(s => new RestApiResponseViewModel
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
                            if (swagger.Metadata.TryGetValue(Constants.PropertyName.Source, out object value))
                            {
                                itemVm.Metadata[Constants.PropertyName.Source] = value;
                            }
                            else
                            {
                                itemVm.Metadata[Constants.PropertyName.Source] = null;
                            }
                            vm.Children.Add(itemVm);
                        }
                    }
                }
            }

            return vm;
        }

        #region Private methods

        private static readonly Regex HtmlEncodeRegex = new Regex(@"\W", RegexOptions.Compiled);
        private const string TagText = "tag";
        private static readonly string[] OperationNames = { "get", "put", "post", "delete", "options", "head", "patch" };

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

        private static string GetUid(SwaggerModel swagger)
        {
            return GenerateUid(swagger.Host, swagger.BasePath, swagger.Info.Title, swagger.Info.Version);
        }

        private static string GetUidForOperation(string parentUid, OperationObject item)
        {
            return GenerateUid(parentUid, item.OperationId);
        }

        private static string GetUidForTag(string parentUid, TagItemObject tag)
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

        /// <summary>
        /// Merge operation's parameters with path's parameters.
        /// </summary>
        /// <param name="operationParameters">Operation's parameters</param>
        /// <param name="pathParameters">Path's parameters</param>
        /// <returns></returns>
        private static IEnumerable<ParameterObject> GetParametersForOperation(List<ParameterObject> operationParameters, List<ParameterObject> pathParameters)
        {
            if (pathParameters == null || pathParameters.Count == 0)
            {
                return operationParameters;
            }
            if (operationParameters == null || operationParameters.Count == 0)
            {
                return pathParameters;
            }

            // Path parameters can be overridden at the operation level.
            var uniquePathParams = pathParameters.Where(
                p => !operationParameters.Any(o => IsParameterEquals(p, o))).ToList();

            return operationParameters.Union(uniquePathParams).ToList();
        }

        /// <summary>
        /// Judge whether two ParameterObject equal to each other. according to value of 'name' and 'in'
        /// Define 'Equals' here instead of inside ParameterObject, since ParameterObject is either self defined or referenced object which 'name' and 'in' needs to be resolved.
        /// </summary>
        /// <param name="left">Fist ParameterObject</param>
        /// <param name="right">Second ParameterObject</param>
        private static bool IsParameterEquals(ParameterObject left, ParameterObject right)
        {
            if (left == null || right == null)
            {
                return false;
            }
            return string.Equals(left.Name, right.Name) &&
                   string.Equals(GetMetadataStringValue(left, "in"), GetMetadataStringValue(right, "in"));
        }

        private static string GetMetadataStringValue(ParameterObject parameter, string metadataName)
        {
            if (parameter.Metadata.TryGetValue(metadataName, out object metadataValue))
            {
                return (string)metadataValue;
            }
            return null;
        }

        #endregion
    }
}
