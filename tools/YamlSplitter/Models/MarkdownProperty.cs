// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.YamlSplitter.Models
{
    using System.Collections.Generic;
    using System.Text;

    public class MarkdownProperty
    {
        public string OPath { get; set; }
        public string Content { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public void SerializeTo(StringBuilder sb)
        {
            sb.Append("## `");
            sb.Append(OPath);
            sb.AppendLine("`");
            sb.AppendLine(Content);
        }
    }
}
