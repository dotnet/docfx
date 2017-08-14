// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System.IO;

    using Newtonsoft.Json.Schema;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using System;
    using System.Collections.Generic;

    public class HrefInterpreter : IInterpreter
    {
        private readonly bool _exportFileLink;
        private readonly bool _updateValue;
        public HrefInterpreter(bool exportFileLink, bool updateValue)
        {
            _exportFileLink = exportFileLink;
            _updateValue = updateValue;
        }

        public bool CanInterpret(BaseSchema schema)
        {
            return schema.ContentType == ContentType.Href && schema.Type == JSchemaType.String;
        }

        public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
        {
            if (!CanInterpret(schema) || value == null)
            {
                return value;
            }

            var val = value as string;
            if (val == null)
            {
                throw new ArgumentException($"{value.GetType()} is not supported type string.");
            }

            if (!Uri.TryCreate(val, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                throw new DocumentException($"{val} is not a valid href");
            }

            // "/" is also considered as absolute to us
            if (uri.IsAbsoluteUri || val.StartsWith("/"))
            {
                return value;
            }

            // sample value: a/b/c?hello
            var filePath = UriUtility.GetPath(val);
            var fragments = UriUtility.GetQueryStringAndFragment(val);
            var relPath = RelativePath.TryParse(filePath);
            if (relPath != null)
            {
                var currentFile = (RelativePath)context.Model.OriginalFileAndType.File;
                relPath = (currentFile + relPath.UrlDecode()).GetPathFromWorkingFolder();
                if (_exportFileLink)
                {
                    ((Dictionary<string, List<LinkSourceInfo>>)context.Properties.FileLinkSources).AddFileLinkSource(new LinkSourceInfo
                    {
                        Target = relPath,
                        Anchor = UriUtility.GetFragment(val),
                        SourceFile = context.Model.OriginalFileAndType.File
                    });
                }

                if (_updateValue && context.BuildContext != null)
                {
                    var resolved = (RelativePath)context.BuildContext.GetFilePath(relPath);
                    if (resolved != null)
                    {
                        val = resolved.MakeRelativeTo(((RelativePath)context.Model.File).GetPathFromWorkingFolder()).UrlEncode() + fragments;
                    }
                }
            }

            return val;
        }
    }
}
