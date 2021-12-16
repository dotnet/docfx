// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class JsonSchemaLoader
{
    private readonly FileResolver _fileResolver;
    private readonly Func<Uri, Uri, string?>? _resolveRef;

    public JsonSchemaLoader(FileResolver fileResolver, Func<Uri, Uri, string?>? resolveRef = null)
    {
        _fileResolver = fileResolver;
        _resolveRef = resolveRef;
    }

    public JsonSchema? TryLoadSchema(Package package, PathString path)
    {
        var json = package.TryReadString(path);
        if (json is null)
        {
            return null;
        }

        return LoadSchema(json, ResolvePackageRef);

        string? ResolvePackageRef(Uri baseUrl, Uri refUrl)
        {
            var relativeUrl = baseUrl.MakeRelativeUri(new Uri(refUrl.GetLeftPart(UriPartial.Path)));
            if (relativeUrl.IsAbsoluteUri)
            {
                return null;
            }

            var resolvedPath = Path.Combine(Path.GetDirectoryName(path) ?? "", relativeUrl.ToString());
            return package.TryReadString(new PathString(resolvedPath));
        }
    }

    public JsonSchema LoadSchema(SourceInfo<string> url)
    {
        return LoadSchema(_fileResolver.ReadString(url));
    }

    public JsonSchema LoadSchema(string json, Func<Uri, Uri, string?>? resolveRef = null)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new JsonSchema();
        }

        var token = JToken.Parse(json);
        var schemaResolver = new JsonSchemaResolver(token, ResolveExternalSchema);

        return schemaResolver.ResolveSchema("") ?? throw new InvalidOperationException();

        JsonSchema? ResolveExternalSchema(Uri baseUrl, Uri refUrl)
        {
            var json = resolveRef?.Invoke(baseUrl, refUrl) ??
                _resolveRef?.Invoke(baseUrl, refUrl) ??
                _fileResolver.TryReadString(new SourceInfo<string>(refUrl.GetLeftPart(UriPartial.Path)));

            return json is null ? null : LoadSchema(json);
        }
    }
}
