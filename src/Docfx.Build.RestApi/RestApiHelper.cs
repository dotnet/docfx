// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.RestApi.Swagger.Internals;
using Docfx.Common;

using Newtonsoft.Json.Linq;

namespace Docfx.Build.RestApi;

internal static class RestApiHelper
{
    private const string JsonExtension = ".json";

    /// <summary>
    /// Reverse to reference unescape described in http://tools.ietf.org/html/rfc6901#section-4
    /// </summary>
    /// <param name="reference"></param>
    /// <returns></returns>
    public static string FormatDefinitionSinglePath(string reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        return reference.Replace("~", "~0").Replace("/", "~1");
    }

    /// <summary>
    /// When the reference starts with '#/', treat it as URI Fragment Identifier Representation and decode.
    /// When the reference starts with '/', treat it as JSON String Representation and keep it as.
    /// Refer to: https://tools.ietf.org/html/rfc6901#section-5
    /// </summary>
    /// <param name="reference"></param>
    /// <returns></returns>
    public static SwaggerFormattedReference FormatReferenceFullPath(string reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        // Decode for URI Fragment Identifier Representation
        if (reference.StartsWith("#/", StringComparison.Ordinal))
        {
            var result = ParseReferencePath(reference.Substring(2));
            return new SwaggerFormattedReference
            {
                Type = SwaggerFormattedReferenceType.InternalReference,
                Path = "/" + result.Item1,
                Name = result.Item2
            };
        }

        // Not decode for JSON String Representation
        if (reference.StartsWith('/'))
        {
            return new SwaggerFormattedReference
            {
                Type = SwaggerFormattedReferenceType.InternalReference,
                Path = reference,
                Name = reference.Split('/').Last()
            };
        }

        // External reference
        if (PathUtility.IsRelativePath(reference))
        {
            // For example "file.json"
            if (reference.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase))
            {
                return new SwaggerFormattedReference
                {
                    Type = SwaggerFormattedReferenceType.ExternalReference,
                    ExternalFilePath = reference,
                    Name = Path.GetFileNameWithoutExtension(reference)
                };
            }

            // For example "file.json#/definitions/reference"
            if (reference.Contains('#'))
            {
                var values = reference.Split('#');
                if (values.Length != 2)
                {
                    throw new InvalidOperationException($"Reference path '{reference}' should contain only one '#' character.");
                }
                var filePath = values[0];
                if (!filePath.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"External file path '{filePath}' should end with {JsonExtension}");
                }
                var parsedFilePath = ParseReferencePath(filePath).Item1;
                var parsedReferencePath = ParseReferencePath(values[1].Substring(1));
                return new SwaggerFormattedReference
                {
                    Type = SwaggerFormattedReferenceType.ExternalEmbeddedReference,
                    ExternalFilePath = parsedFilePath,
                    Path = "/" + parsedReferencePath.Item1,
                    Name = parsedReferencePath.Item2
                };
            }
        }

        throw new InvalidOperationException($"Reference path \"{reference}\" is not supported now.");
    }

    public static void CheckSpecificKey(JToken jToken, string key, Action action)
    {
        if (jToken is JArray jArray)
        {
            foreach (var item in jArray)
            {
                CheckSpecificKey(item, key, action);
            }
        }

        if (jToken is JObject jObject)
        {
            foreach (var pair in jObject)
            {
                if (pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    action();
                }
                CheckSpecificKey(pair.Value, key, action);
            }
        }
    }

    private static Tuple<string, string> ParseReferencePath(string path)
    {
        if (!RelativePath.IsRelativePath(path))
        {
            throw new InvalidOperationException($"{path} should be relative path.");
        }

        // Reuse relative path, to decode the values inside '/'.
        var decodedPath = ((RelativePath)path).UrlDecodeUnsafe();

        return Tuple.Create(decodedPath.ToString(), decodedPath.FileName);
    }
}
