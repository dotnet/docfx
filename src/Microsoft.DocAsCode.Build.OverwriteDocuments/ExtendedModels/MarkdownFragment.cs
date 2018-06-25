// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    using Microsoft.DocAsCode.Common;

    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class MarkdownFragment
    {
        public string Uid { get; set; }

        public Dictionary<string, object> Metadata { get; set; }

        public Dictionary<string, MarkdownProperty> Properties { get; set; }

        public bool Touched { get; set; }

        public override string ToString()
        {
            string uidWrapper = OverwriteUtility.GetUidWrapper(Uid);
            StringBuilder sb = new StringBuilder();
            sb.Append("# " + uidWrapper);
            sb.Append(Uid);
            sb.AppendLine(uidWrapper);

            if (Metadata?.Count > 0)
            {
                SerializeYamlHeader(Metadata, sb);
            }
            sb.AppendLine();

            foreach (var prop in Properties.Values)
            {
                prop.SerializeTo(sb);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void SerializeYamlHeader(Dictionary<string, object> metadata, StringBuilder sb)
        {
            if (metadata?.Count > 0)
            {
                sb.AppendLine("```yaml");
                YamlUtility.Serialize(new StringWriter(sb), metadata);
                sb.AppendLine("```");
            }
        }
    }
}
