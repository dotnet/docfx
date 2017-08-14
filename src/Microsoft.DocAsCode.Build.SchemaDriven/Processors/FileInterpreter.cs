// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json.Schema;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

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
            return schema.ContentType == ContentType.File && schema.Type == JSchemaType.String;
        }

        public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
        {
            if (!CanInterpret(schema) || value == null)
            {
                return value;
            }

            if (!(value is string val))
            {
                throw new ArgumentException($"{value.GetType()} is not supported type string.");
            }

            var relPath = RelativePath.TryParse(val) ?? throw new DocumentException($"{val} is not a valid relative file path that supported by contentType file ");

            var currentFile = (RelativePath)context.Model.OriginalFileAndType.File;
            relPath = (currentFile + relPath).GetPathFromWorkingFolder();
            if (_exportFileLink)
            {
                ((Dictionary<string, List<LinkSourceInfo>>)context.Properties.FileLinkSources).AddFileLinkSource(new LinkSourceInfo
                {
                    Target = relPath,
                    SourceFile = context.Model.OriginalFileAndType.File
                });
            }

            if (_updateValue && context.BuildContext != null)
            {
                var resolved = (RelativePath)context.BuildContext.GetFilePath(relPath);
                if (resolved != null)
                {
                    val = resolved.MakeRelativeTo(((RelativePath)context.Model.File).GetPathFromWorkingFolder());
                }
            }

            return val;
        }
    }
}
