// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class PublicTemplatePackage : Package
{
    private readonly Uri _baseUrl;
    private readonly FileResolver _fileResolver;

    public PublicTemplatePackage(string baseUrl, FileResolver fileResolver)
    {
        _baseUrl = new($"{baseUrl.TrimEnd('/')}/");
        _fileResolver = fileResolver;
    }

    public override PathString BasePath => throw new NotSupportedException();

    public override bool Exists(PathString path)
    {
        return _fileResolver.TryResolveFilePath(GetPath(path), out _);
    }

    public override IEnumerable<PathString> GetFiles(PathString directory = default, string[]? allowedFileNames = null)
        => throw new NotSupportedException();

    public override PathString GetFullFilePath(PathString path) => throw new NotSupportedException();

    public override DateTime? TryGetLastWriteTimeUtc(PathString path) => throw new NotSupportedException();

    public override byte[] ReadBytes(PathString path) => _fileResolver.ReadBytes(GetPath(path));

    public override Stream ReadStream(PathString path) => _fileResolver.ReadStream(GetPath(path));

    public override PathString? TryGetPhysicalPath(PathString path) => throw new NotSupportedException();

    private SourceInfo<string> GetPath(PathString path)
    {
        if (path.StartsWithPath(new PathString("ContentTemplate"), out var remainingPath))
        {
            path = remainingPath;
        }
        return new SourceInfo<string>(new Uri(_baseUrl, path.Value).AbsoluteUri);
    }
}
