// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Docfx.Build.OverwriteDocuments;

public class MarkdownProperty
{
    public string OPath { get; set; }
    public string Content { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public bool Touched { get; set; }
    public void SerializeTo(StringBuilder sb)
    {
        string wrapper = OverwriteUtility.GetUidWrapper(OPath);
        sb.Append("## " + wrapper);
        sb.Append(OPath);
        sb.AppendLine(wrapper);
        sb.AppendLine(Content);
    }
}
