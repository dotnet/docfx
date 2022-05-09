// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Parsing;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class CommandLineOptions
{
    internal static class UI
    {
        public static readonly Option<string> Output = new(
            new[] { "-o", "--output" }, "Output directory in which to place built artifacts.");

        public static readonly Option<bool> Force = new(
            "--force", "Forces content to be generated even if it would change existing files.");

        public static readonly Option<bool> Stdin = new(
            "--stdin", "Enable additional config in JSON one liner using standard input.");

        public static readonly Option<bool> Verbose = new(
            new[] { "-v", "--verbose" }, "Enable diagnostics console output.");

        public static readonly Option<string> Log = new(
            "--log", "Enable logging to the specified file path.");

        public static readonly Option<string> Template = new(
            "--template", "The directory or git repository that contains website template.");

        public static readonly Option<string[]> File = new(
            new[] { "--file" }, "Build only the specified files.");

        public static readonly Option<OutputType> OutputType = new(
            "--output-type", "Output directory in which to place built artifacts.");

        public static readonly Option<bool> DryRun = new(
            "--dry-run", "Do not produce build artifact and only produce validation result.");

        public static readonly Option<bool> NoDrySync = new(
            "--no-dry-sync", "Do not run dry sync for learn validation.");

        public static readonly Option<bool> NoRestore = new(
            "--no-restore", "Do not restore dependencies before build.");

        public static readonly Option<bool> NoCache = new(
            "--no-cache", "Always fetch latest dependencies in build.");

        public static readonly Option<string> TemplateBasePath = new(
            "--template-base-path", "The base path used for referencing the template resource file when applying liquid.");

        public static readonly Option<bool> Continue = new(
            "--continue", "Continue build based on intermediate json output.");

        public static readonly Option<string> Locale = new(
            "--locale", "Locale info for continue build.");

        public static readonly Option<string> Address = new(
            "--address", () => "127.0.0.1", "Address to use.");

        public static readonly Option<int> Port = new(
            "--port", () => 8080, "Port to use. If 0, look for open port.");

        public static readonly Option<bool> LanguageServer = new(
            "--language-server", "Starts a language server.");

        public static Argument<string> TemplateName = new(
            "templateName", "Docset template name") { Arity = ArgumentArity.ZeroOrOne };

        public static Argument<string> Directory = new(
            "directory", "A directory that contains docfx.yml/docfx.json.") { Arity = ArgumentArity.ZeroOrOne };
    }

    public CommandLineOptions()
    {
    }

    public CommandLineOptions(ParseResult res)
    {
        Locale = res.GetValueForOption(UI.Locale);
        Continue = res.GetValueForOption(UI.Continue);
        Output = res.GetValueForOption(UI.Output);
        Log = res.GetValueForOption(UI.Log);
        Verbose = res.GetValueForOption(UI.Verbose);
        OutputType = res.GetValueForOption(UI.OutputType);
        DryRun = res.GetValueForOption(UI.DryRun);
        NoDrySync = res.GetValueForOption(UI.NoDrySync);
        Stdin = res.GetValueForOption(UI.Stdin);
        Force = res.GetValueForOption(UI.Force);
        NoCache = res.GetValueForOption(UI.NoCache);
        NoRestore = res.GetValueForOption(UI.NoRestore);
        LanguageServer = res.GetValueForOption(UI.LanguageServer);
        Port = res.GetValueForOption(UI.Port);
        Address = res.GetValueForOption(UI.Address);
        Template = res.GetValueForOption(UI.Template);
        TemplateBasePath = res.GetValueForOption(UI.TemplateBasePath);
        File = res.GetValueForOption(UI.File);
        TemplateName = res.GetValueForArgument(UI.TemplateName);
        Directory = res.GetValueForArgument(UI.Directory);
    }

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
