// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.SchemaDriven.Processors;

public class FileInterpreter : IInterpreter
{
    private readonly bool _exportFileLink;
    private readonly bool _updateValue;

    public FileInterpreter(bool exportFileLink, bool updateValue)
    {
        _exportFileLink = exportFileLink;
        _updateValue = updateValue;
    }

    public bool CanInterpret(BaseSchema schema)
    {
        return schema is { ContentType: ContentType.File };
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

        var relPath = RelativePath.TryParse(val);
        if (relPath == null)
        {
            var message = $"{val} is not a valid relative file path that supported by contentType file ";
            Logger.LogError(message, code: ErrorCodes.Build.InvalidRelativePath);
            throw new DocumentException(message);
        }

        var originalFile = context.GetOriginalContentFile(path);

        var currentFile = (RelativePath)originalFile.File;
        relPath = (currentFile + relPath).GetPathFromWorkingFolder();
        if (_exportFileLink)
        {
            context.FileLinkSources.AddFileLinkSource(new LinkSourceInfo
            {
                Target = relPath,
                SourceFile = originalFile.File
            });
        }

        if (_updateValue && context.BuildContext != null)
        {
            var resolved = (RelativePath)context.BuildContext.GetFilePath(relPath);
            if (resolved != null)
            {
                val = resolved.MakeRelativeTo(((RelativePath)context.FileAndType.File).GetPathFromWorkingFolder());
            }
        }

        return val;
    }
}
