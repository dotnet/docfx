﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Plugins;

    public class XrefInterpreter : IInterpreter
    {
        public bool CanInterpret(BaseSchema schema)
        {
            return schema != null && schema.ContentType == ContentType.Xref;
        }

        public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
        {
            if (value == null || !CanInterpret(schema))
            {
                return value;
            }

            if (!(value is string val))
            {
                throw new ArgumentException($"{value.GetType()} is not supported type string.");
            }

            AddUidLinkSource(context.UidLinkSources, new LinkSourceInfo
            {
                Target = val,
                SourceFile = context.OriginalFileAndType.File
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
