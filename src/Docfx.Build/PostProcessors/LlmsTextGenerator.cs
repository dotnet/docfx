// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Text;

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

[Export(nameof(LlmsTextGenerator), typeof(IPostProcessor))]
class LlmsTextGenerator : IPostProcessor
{
    private const string HtmlExtension = ".html";
    private const string LlmsFileName = "llms.txt";

    public string Name => nameof(LlmsTextGenerator);

    public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
    {
        return metadata;
    }

    public Manifest Process(Manifest manifest, string outputFolder, CancellationToken cancellationToken = default)
    {
        if (manifest.LlmsText == null || string.IsNullOrEmpty(manifest.LlmsText.Title))
        {
            return manifest;
        }

        var content = GenerateLlmsTextContent(manifest);

        var llmsTextOutputFile = Path.Combine(outputFolder, LlmsFileName);
        Logger.LogInfo($"llms.txt file is successfully exported to {llmsTextOutputFile}");
        File.WriteAllText(llmsTextOutputFile, content);
        return manifest;
    }

    private static string GenerateLlmsTextContent(Manifest manifest)
    {
        var options = manifest.LlmsText;
        var builder = new StringBuilder();

        // Required H1 with title
        builder.AppendLine($"# {options.Title}");
        builder.AppendLine();

        // Optional blockquote with summary
        if (!string.IsNullOrEmpty(options.Summary))
        {
            builder.AppendLine($"> {options.Summary}");
            builder.AppendLine();
        }

        // Optional details
        if (!string.IsNullOrEmpty(options.Details))
        {
            builder.AppendLine(options.Details);
            builder.AppendLine();
        }

        // Optional sections with links
        if (options.Sections != null && options.Sections.Count > 0)
        {
            foreach (var section in options.Sections)
            {
                builder.AppendLine($"## {section.Key}");
                builder.AppendLine();

                if (section.Value != null)
                {
                    foreach (var link in section.Value)
                    {
                        builder.Append($"- [{link.Title}]({link.Url})");
                        if (!string.IsNullOrEmpty(link.Description))
                        {
                            builder.Append($": {link.Description}");
                        }
                        builder.AppendLine();
                    }
                    builder.AppendLine();
                }
            }
        }

        return builder.ToString();
    }
}