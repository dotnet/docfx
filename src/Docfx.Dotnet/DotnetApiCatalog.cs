// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Docfx.Common;
using Docfx.Plugins;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;

namespace Docfx.Dotnet;

/// <summary>
/// Provides access to a .NET API definitions and their associated documentation.
/// </summary>
public static partial class DotnetApiCatalog
{
    private static IDeserializer deserializer = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build();

    /// <summary>
    /// Generates metadata reference YAML files using docfx.json config.
    /// </summary>
    /// <param name="configPath">The path to docfx.json config file.</param>
    /// <returns>A task to await for build completion.</returns>
    public static Task GenerateManagedReferenceYamlFiles(string configPath)
    {
        return GenerateManagedReferenceYamlFiles(configPath, new());
    }

    /// <summary>
    /// Generates metadata reference YAML files using docfx.json config.
    /// </summary>
    /// <param name="configPath">The path to docfx.json config file.</param>
    /// <returns>A task to await for build completion.</returns>
    public static async Task GenerateManagedReferenceYamlFiles(string configPath, DotnetApiOptions options)
    {
        var consoleLogListener = new ConsoleLogListener();
        Logger.RegisterListener(consoleLogListener);

        try
        {
            var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath));
            var config = JObject.Parse(await File.ReadAllTextAsync(configPath));
            if (config.TryGetValue("metadata", out var value))
            {
                Logger.Rules = config["rules"]?.ToObject<Dictionary<string, LogLevel>>();
                await Exec(
                    value.ToObject<MetadataJsonConfig>(NewtonsoftJsonUtility.DefaultSerializer.Value),
                    options,
                    configDirectory,
                    assemblyUids: config["assemblyUids"]?.ToObject<AssemblyUidConfig>(NewtonsoftJsonUtility.DefaultSerializer.Value));
            }
        }
        finally
        {
            Logger.Flush();
            Logger.PrintSummary();
            Logger.UnregisterAllListeners();
        }
    }

    internal static async Task Exec(
        MetadataJsonConfig config,
        DotnetApiOptions options,
        string configDirectory,
        string outputDirectory = null,
        AssemblyUidConfig assemblyUids = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var originalGlobalNamespaceId = VisitorHelper.GlobalNamespaceId;
        var originalAssemblyUids = VisitorHelper.AssemblyUids;
        var originalAssemblyUidOverride = VisitorHelper.AssemblyUidOverride;
        var originalAssemblyUidOverrideAssemblies = VisitorHelper.AssemblyUidOverrideAssemblies;

        try
        {
            EnvironmentContext.SetBaseDirectory(configDirectory);

            // Whether an assembly is qualified is a property of the assembly, not of the metadata item that
            // documents it, which is why this is a project level setting: every item has to agree for the
            // references it makes into assemblies documented by another item to resolve.
            VisitorHelper.AssemblyUids = ValidateAssemblyUids(assemblyUids);
            ValidateAssemblyUidOverrides(config);

            foreach (var item in config)
            {
                VisitorHelper.GlobalNamespaceId = item.GlobalNamespaceId;
                EnvironmentContext.SetGitFeaturesDisabled(item.DisableGitFeatures);

                await Build(ConvertConfig(item, configDirectory, outputDirectory), options);
            }
        }
        finally
        {
            VisitorHelper.GlobalNamespaceId = originalGlobalNamespaceId;
            VisitorHelper.AssemblyUids = originalAssemblyUids;
            VisitorHelper.AssemblyUidOverride = originalAssemblyUidOverride;
            VisitorHelper.AssemblyUidOverrideAssemblies = originalAssemblyUidOverrideAssemblies;
            EnvironmentContext.Clean();
        }

        Logger.LogVerbose($".NET API done in {stopwatch.Elapsed}");

        async Task Build(ExtractMetadataConfig config, DotnetApiOptions options)
        {
            var assemblies = await Compile(config);

            // `assemblyUidOverride` applies to the assemblies this metadata item documents, which are only
            // known once they are compiled. It stays constant for the whole item, so the parallel
            // API page generation can read it safely.
            VisitorHelper.AssemblyUidOverride = config.AssemblyUidOverride;
            VisitorHelper.AssemblyUidOverrideAssemblies = string.IsNullOrEmpty(config.AssemblyUidOverride)
                ? null
                : new HashSet<IAssemblySymbol>(assemblies.Select(a => a.symbol), SymbolEqualityComparer.Default);

            switch (config.OutputFormat)
            {
                case MetadataOutputFormat.Markdown:
                    CreatePages(WriteMarkdown, assemblies, config, options);

                    void WriteMarkdown(string outputFolder, string id, Build.ApiPage.ApiPage apiPage)
                    {
                        File.WriteAllText(Path.Combine(outputFolder, $"{id}.md"), Docfx.Build.ApiPage.ApiPageMarkdownTemplate.Render(apiPage));
                    }
                    break;

                case MetadataOutputFormat.ApiPage:
                    CreatePages(WriteYaml, assemblies, config, options);

                    void WriteYaml(string outputFolder, string id, Build.ApiPage.ApiPage apiPage)
                    {
                        var json = JsonSerializer.Serialize(apiPage, Docfx.Build.ApiPage.ApiPage.JsonSerializerOptions);
                        var obj = deserializer.Deserialize(json);
                        YamlUtility.Serialize(Path.Combine(outputFolder, $"{id}.yml"), obj, "YamlMime:ApiPage");
                    }
                    break;

                case MetadataOutputFormat.Mref:
                    CreateManagedReference(assemblies, config, options);
                    break;
            }
        }
    }

    // A UID ends up as a file name, an xref key and an HTML anchor, so the assembly component is
    // restricted to the characters that are safe in all three: letters, digits, underscores and dashes,
    // with dots as separators. Dashes are allowed because assembly names, which are the components in the
    // normal case, often contain them. Unlike a namespace, a segment may start with a digit, so both
    // `7zip.Net` style assembly names and `net8.0` style components work.
    [GeneratedRegex(@"^[A-Za-z0-9_\-]+(\.[A-Za-z0-9_\-]+)*$")]
    private static partial Regex AssemblyUidRegex();

    private const string AssemblyUidGrammar =
        "An assembly component may contain letters, digits, underscores and dashes, separated by dots, e.g. 'MyLib', 'MyLib.V2' or 'net8.0'.";

    private static bool IsValidAssemblyUid(string assemblyUid)
    {
        return !string.IsNullOrEmpty(assemblyUid) && AssemblyUidRegex().IsMatch(assemblyUid);
    }

    /// <summary>
    /// Drops invalid entries from the project level <c>assemblyUids</c> and makes lookups by assembly name
    /// case insensitive, as assembly names are.
    /// </summary>
    private static Dictionary<string, string> ValidateAssemblyUids(AssemblyUidConfig assemblyUids)
    {
        if (assemblyUids is null)
        {
            return null;
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (assemblyName, component) in assemblyUids)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                Logger.LogWarning("Ignoring 'assemblyUids' entry with an empty assembly name.", code: "InvalidAssemblyUid");
                continue;
            }

            // A null component means the assembly is qualified by its own name, so the name itself has to
            // be usable as a component. It is checked here rather than once per symbol, as the configured
            // name and the real one differ at most in casing.
            if (!IsValidAssemblyUid(component ?? assemblyName))
            {
                Logger.LogWarning(
                    component is null
                        ? $"Ignoring 'assemblyUids' entry '{assemblyName}', which cannot be used as an assembly component. {AssemblyUidGrammar}"
                        : $"Ignoring invalid assembly component '{component}' for assembly '{assemblyName}'. {AssemblyUidGrammar}",
                    code: "InvalidAssemblyUid");
                continue;
            }

            if (result.TryGetValue(assemblyName, out var existingComponent))
            {
                if (existingComponent != component)
                {
                    Logger.LogWarning(
                        $"Assembly '{assemblyName}' is mapped to both assembly component '{existingComponent}' and '{component}', '{existingComponent}' is used.",
                        code: "InvalidAssemblyUid");
                }
                continue;
            }

            result.Add(assemblyName, component);
        }

        return result;
    }

    /// <summary>
    /// Drops each metadata item's <c>assemblyUidOverride</c> if it isn't a usable assembly component.
    /// </summary>
    private static void ValidateAssemblyUidOverrides(MetadataJsonConfig config)
    {
        foreach (var item in config)
        {
            if (item.AssemblyUidOverride is not null && !IsValidAssemblyUid(item.AssemblyUidOverride))
            {
                Logger.LogWarning(
                    $"Ignoring invalid assembly component '{item.AssemblyUidOverride}'. {AssemblyUidGrammar}",
                    code: "InvalidAssemblyUid");
                item.AssemblyUidOverride = null;
            }
        }
    }

    private static ExtractMetadataConfig ConvertConfig(MetadataJsonItemConfig configModel, string configDirectory, string outputDirectory)
    {
        var projects = configModel.Src;
        var references = configModel.References;

        var outputFolder = Path.GetFullPath(Path.Combine(
            string.IsNullOrEmpty(outputDirectory) ? Path.Combine(configDirectory, configModel.Output ?? "") : outputDirectory,
            configModel.Dest ?? ""));

        var expandedFiles = GlobUtility.ExpandFileMapping(EnvironmentContext.BaseDirectory, projects);
        var expandedReferences = GlobUtility.ExpandFileMapping(EnvironmentContext.BaseDirectory, references);

        ExtractMetadataConfig.UseClrTypeNames = configModel?.UseClrTypeNames ?? false;

        return new ExtractMetadataConfig
        {
            ShouldSkipMarkup = configModel?.ShouldSkipMarkup ?? false,
            FilterConfigFile = configModel?.Filter is null ? null : Path.GetFullPath(Path.Combine(EnvironmentContext.BaseDirectory, configModel.Filter)),
            IncludePrivateMembers = configModel?.IncludePrivateMembers ?? false,
            IncludeExplicitInterfaceImplementations = configModel?.IncludeExplicitInterfaceImplementations ?? false,
            GlobalNamespaceId = configModel?.GlobalNamespaceId,
            AssemblyUidOverride = configModel?.AssemblyUidOverride,
            AssemblyLabel = configModel?.AssemblyLabel ?? default,
            MSBuildProperties = configModel?.Properties,
            OutputFormat = configModel?.OutputFormat ?? default,
            OutputFolder = outputFolder,
            CodeSourceBasePath = configModel?.CodeSourceBasePath,
            DisableDefaultFilter = configModel?.DisableDefaultFilter ?? false,
            DisableGitFeatures = configModel?.DisableGitFeatures ?? false,
            NoRestore = configModel?.NoRestore ?? false,
            CategoryLayout = configModel?.CategoryLayout ?? default,
            NamespaceLayout = configModel?.NamespaceLayout ?? default,
            MemberLayout = configModel?.MemberLayout ?? default,
            EnumSortOrder = configModel?.EnumSortOrder ?? default,
            AllowCompilationErrors = configModel?.AllowCompilationErrors ?? false,
            Files = expandedFiles.Items.SelectMany(static s => s.Files).ToList(),
            References = expandedReferences?.Items.SelectMany(static s => s.Files).ToList()
        };
    }
}
