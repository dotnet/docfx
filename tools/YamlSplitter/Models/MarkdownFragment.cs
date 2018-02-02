// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.YamlSplitter.Models
{
    using System.Collections.Generic;
    using System.Text;

    public class MarkdownFragment
    {
        public string Uid { get; set; }

        public Dictionary<string, object> Metadata { get; set; }

        public Dictionary<string, MarkdownProperty> Properties { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("# `");
            sb.Append(Uid);
            sb.AppendLine("`");

            if (Metadata?.Count > 0)
            {
                SerializeYamlHeader(Metadata, sb);
            }

            foreach(var prop in Properties.Values)
            {
                prop.SerializeTo(sb);
            }

            return sb.ToString();
        }

        private static void SerializeYamlHeader(Dictionary<string, object> metadata, StringBuilder sb)
        {
            if (metadata?.Count > 0)
            {
                sb.AppendLine("```yaml");
                var serializer = new YamlDotNet.Serialization.Serializer();
                sb.AppendLine(serializer.Serialize(metadata));
                sb.AppendLine("```");
            }
        }
    }
}
