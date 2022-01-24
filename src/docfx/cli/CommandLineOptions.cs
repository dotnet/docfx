// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class CommandLineOptions
{
    public string? Locale { get; init; }

    public bool Continue { get; init; }

    public string? Output { get; init; }

    public string? Log { get; init; }

    public bool Verbose { get; init; }

    public OutputType? OutputType { get; init; }

    public bool DryRun { get; set; }

    public bool NoDrySync { get; init; }

    public bool Stdin { get; init; }

    public bool Force { get; init; }

    public bool NoCache { get; init; }

    public bool NoRestore { get; init; }

    public bool LanguageServer { get; init; }

    public int Port { get; init; }

    public string? Address { get; init; }

    public string? Template { get; init; }

    public string? TemplateBasePath { get; init; }

    public IReadOnlyList<string>? File { get; init; }

    public string? TemplateName { get; init; }

    public string? Directory { private get; set; }

    public string WorkingDirectory => Directory ?? ".";

    public JObject? StdinConfig { get; set; }

    public JObject ToJObject()
    {
        var config = new JObject
        {
            ["dryRun"] = DryRun,
            ["noDrySync"] = NoDrySync,
        };

        if (Output != null)
        {
            config["outputPath"] = Path.GetFullPath(Output);
        }

        if (OutputType != null)
        {
            config["outputType"] = OutputType.Value.ToString();
        }

        if (Template != null)
        {
            config["template"] = new PackagePath(Template).Type switch
            {
                PackageType.Folder => Path.GetFullPath(Template),
                _ => Template,
            };
        }

        if (TemplateBasePath != null)
        {
            config["templateBasePath"] = TemplateBasePath;
        }

        return config;
    }
}
