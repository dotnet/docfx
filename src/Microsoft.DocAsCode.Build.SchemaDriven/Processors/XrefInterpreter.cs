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

    public class XrefInterpreter : IInterpreter
    {
        public bool CanInterpret(BaseSchema schema)
        {
            return schema.ContentType == ContentType.Xref && schema.Type == JSchemaType.String;
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

            AddUidLinkSource(context.Properties.UidLinkSources, new LinkSourceInfo
            {
                Target = val,
                SourceFile = context.Model.OriginalFileAndType.File
            });

            return value;
        }

        private void AddUidLinkSource(Dictionary<string, List<LinkSourceInfo>> uidLinkSources, LinkSourceInfo source)
        {
            var file = source.Target;
            if (!uidLinkSources.TryGetValue(file, out List<LinkSourceInfo> sources))
            {
                sources = new List<LinkSourceInfo>();
                uidLinkSources[file] = sources;
            }
            sources.Add(source);
        }
    }
}
