// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Docfx.Common;

namespace Docfx.Build.OverwriteDocuments;

public class MarkdownFragment
{
    public string Uid { get; set; }

    public Dictionary<string, object> Metadata { get; set; }

    public Dictionary<string, MarkdownProperty> Properties { get; set; }

    public bool Touched { get; set; }

    public override string ToString()
    {
        string uidWrapper = OverwriteUtility.GetUidWrapper(Uid);
        StringBuilder sb = new();
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
