// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.SchemaDriven.Processors;

public class FileIncludeInterpreter : IInterpreter
{
    public bool CanInterpret(BaseSchema schema)
    {
        return schema != null && schema.Reference != ReferenceType.None;
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

        var filePath = val;
        var relPath = RelativePath.TryParse(val);
        if (relPath != null)
        {
            var currentFile = (RelativePath)context.OriginalFileAndType.File;
            filePath = currentFile + relPath;
        }

        context.SetOriginalContentFile(path, new FileAndType(context.OriginalFileAndType.BaseDir, filePath, DocumentType.Article));

        return EnvironmentContext.FileAbstractLayer.ReadAllText(filePath);
    }
}
