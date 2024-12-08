// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.SchemaDriven.Processors;

public class HrefInterpreter : IInterpreter
{
    private readonly bool _exportFileLink;
    private readonly bool _updateValue;
    private readonly string _siteHostName;

    public HrefInterpreter(bool exportFileLink, bool updateValue, string siteHostName = null)
    {
        _exportFileLink = exportFileLink;
        _updateValue = updateValue;
        _siteHostName = siteHostName;
    }

    public bool CanInterpret(BaseSchema schema)
    {
        return schema is { ContentType: ContentType.Href };
    }

    public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
    {
        if (value == null || !CanInterpret(schema))
        {
            return value;
        }

        if (value is not string val)
        {
            throw new ArgumentException($"{value.GetType()} is not supported type string.");
        }

        if (!Uri.TryCreate(val, UriKind.RelativeOrAbsolute, out Uri uri))
        {
            var message = $"{val} is not a valid href";
            Logger.LogError(message, code: ErrorCodes.Build.InvalidHref);
            throw new DocumentException(message);
        }

        // "/" is also considered as absolute to us
        if (uri.IsAbsoluteUri || val.StartsWith('/'))
        {
            return Helper.RemoveHostName(val, _siteHostName);
        }

        // sample value: a/b/c?hello
        var filePath = UriUtility.GetPath(val);
        var fragments = UriUtility.GetQueryStringAndFragment(val);
        var relPath = RelativePath.TryParse(filePath);
        if (relPath != null)
        {
            var originalFile = context.GetOriginalContentFile(path);
            var currentFile = (RelativePath)originalFile.File;
            relPath = (currentFile + relPath.UrlDecode()).GetPathFromWorkingFolder();
            if (_exportFileLink)
            {
                context.FileLinkSources.AddFileLinkSource(new LinkSourceInfo
                {
                    Target = relPath,
                    Anchor = UriUtility.GetFragment(val),
                    SourceFile = originalFile.File
                });
            }

            if (_updateValue && context.BuildContext != null)
            {
                var resolved = (RelativePath)context.BuildContext.GetFilePath(relPath);
                if (resolved != null)
                {
                    val = resolved.MakeRelativeTo(((RelativePath)context.FileAndType.File).GetPathFromWorkingFolder()).UrlEncode() + fragments;
                }
            }
        }

        return val;
    }
}
