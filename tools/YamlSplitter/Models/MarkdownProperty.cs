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
            string wrapper = FragmentModelHelper.GetUidWrapper(OPath);
            sb.Append("## " + wrapper);
            sb.Append(OPath);
            sb.AppendLine(wrapper);
            sb.AppendLine(Content);
        }
    }
}
