// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

using YamlDotNet.Serialization;

namespace Microsoft.Docs.Build
{
    internal static class LegacyXrefMap
    {
        // todo: generate real xref map
        public static void Convert(Docset docset, Context context)
        {
            var map = new
            {
                sorted = true,
                references = Array.Empty<string>(),
            };

            using (var writer = new StreamWriter(context.WriteStream(Path.Combine(docset.Config.SiteBasePath, "xrefmap.yml"))))
            {
                var sb = new StringBuilder();
                sb.AppendLine("### YamlMime:XRefMap");
                var serializer = new SerializerBuilder().Build();
                var str = serializer.Serialize(map);
                sb.AppendLine(str);
                writer.Write(sb.ToString());
            }
        }
    }
}
