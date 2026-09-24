// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.RestApi.Swagger;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.RestApi;

using Newtonsoft.Json.Linq;

using static Docfx.Build.RestApi.RestApiModelUtility;

namespace Docfx.Build.RestApi;

public static partial class SwaggerModelConverter
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
            Children = [],
            Raw = swagger.Raw,
            Tags = []
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
                        if (op.Value is not JObject opJObject)
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
                        itemVm.Metadata[Constants.PropertyName.Source] = swagger.Metadata.GetValueOrDefault(Constants.PropertyName.Source);
                        vm.Children.Add(itemVm);
                    }
                }
            }
        }

        return vm;
    }

    internal static RestApiRootItemViewModel Convert(SwaggerModel swagger)
    {
        var model = FromSwaggerModel(swagger);
        model.SpecificationVersion = "2.0";
        model.SecurityDefinitions = Take<Dictionary<string, RestApiSecuritySchemeViewModel>>(model.Metadata, "securityDefinitions");
        model.Info = JObject.FromObject(swagger.Info).ToObject<RestApiInfoViewModel>();
        model.ExternalDocs = Take<RestApiExternalDocumentationViewModel>(model.Metadata, "externalDocs");
        foreach (var child in model.Children)
        {
            // Preserve the established Swagger URL display convention at the adapter boundary.
            var query = child.Parameters?.Where(p => (string)p.Metadata.GetValueOrDefault("in") == "query").ToList() ?? [];
            var required = query.Where(p => p.Metadata.GetValueOrDefault("required") is true).Select(p => p.Name).ToList();
            var optional = query.Where(p => p.Metadata.GetValueOrDefault("required") is not true).Select(p => p.Name).ToList();
            child.DisplayPath = child.Path + (required.Count > 0 ? "?" + string.Join('&', required) : "") +
                (optional.Count > 0 ? "[" + (required.Count > 0 ? "&" : "?") + string.Join('&', optional) + "]" : "");
            foreach (var parameter in child.Parameters ?? [])
            {
                parameter.Schema = Take<RestApiSchemaViewModel>(parameter.Metadata, "schema");
                if (parameter.Schema == null)
                {
                    parameter.Schema = JObject.FromObject(parameter.Metadata).ToObject<RestApiSchemaViewModel>();
                }
                SetReferenceIds(parameter.Schema);
            }
            foreach (var response in child.Responses ?? [])
            {
                response.Schema = Take<RestApiSchemaViewModel>(response.Metadata, "schema");
                response.Headers = Take<Dictionary<string, RestApiSchemaViewModel>>(response.Metadata, "headers");
                SetReferenceIds(response.Schema);
            }
        }
        return model;
    }

    private static T Take<T>(Dictionary<string, object> metadata, string name) where T : class =>
        metadata.Remove(name, out var value) && value != null ? JToken.FromObject(value).ToObject<T>() : null;

    private static void SetReferenceIds(RestApiSchemaViewModel schema)
    {
        if (schema == null) return;
        var name = schema.ReferenceName ?? schema.LoopReferenceName;
        schema.ReferenceId = name?.Replace('.', '_');
        foreach (var property in schema.Properties?.Values.AsEnumerable() ?? []) SetReferenceIds(property);
        foreach (var branch in schema.AllOf ?? []) SetReferenceIds(branch);
        SetReferenceIds(schema.Items);
    }

    #region Private methods

    private const string TagText = "tag";
    private static readonly string[] OperationNames = ["get", "put", "post", "delete", "options", "head", "patch"];

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
    /// Merge operation's parameters with path's parameters.
    /// </summary>
    /// <param name="operationParameters">Operation's parameters</param>
    /// <param name="pathParameters">Path's parameters</param>
    /// <returns></returns>
    private static IEnumerable<ParameterObject> GetParametersForOperation(List<ParameterObject> operationParameters, List<ParameterObject> pathParameters)
    {
        return MergeParameters(operationParameters, pathParameters, IsParameterEquals);
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
