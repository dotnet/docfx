﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System;

    using Newtonsoft.Json.Schema;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class FileIncludeInterpreter : IInterpreter
    {
        public bool CanInterpret(BaseSchema schema)
        {
            return schema.Type == JSchemaType.String && schema.Reference != ReferenceType.None;
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

            var filePath = val;
            var relPath = RelativePath.TryParse(val);
            if (relPath != null)
            {
                var currentFile = (RelativePath)context.Model.OriginalFileAndType.File;
                filePath = currentFile + relPath;
            }

            context.Properties.ContentOriginalFile =
                new FileAndType(context.Model.OriginalFileAndType.BaseDir, filePath, DocumentType.Article);

            return EnvironmentContext.FileAbstractLayer.ReadAllText(filePath);
        }
    }
}
