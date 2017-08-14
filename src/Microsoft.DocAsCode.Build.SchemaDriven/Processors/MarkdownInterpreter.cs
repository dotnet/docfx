// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json.Schema;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using System;

    public class MarkdownInterpreter : IInterpreter
    {
        public bool CanInterpret(BaseSchema schema)
        {
            return schema.ContentType == ContentType.Markdown && schema.Type == JSchemaType.String;
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

            return MarkupCore(val, context);
        }

        private static string MarkupCore(string content, IProcessContext context)
        {
            var host = context.Host;
            var mr = host.Markup(content, context.Model.OriginalFileAndType);
            ((Dictionary<string, List<LinkSourceInfo>>)context.Properties.FileLinkSources).Merge(mr.FileLinkSources);
            ((Dictionary<string, List<LinkSourceInfo>>)context.Properties.UidLinkSources).Merge(mr.UidLinkSources);
            ((HashSet<string>)context.Properties.Dependency).UnionWith(mr.Dependency);
            return mr.Html;
        }
    }
}
