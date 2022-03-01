// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias maml;

using ECMA2Yaml;
using Newtonsoft.Json.Linq;
using ECMALogItem = ECMA2Yaml.LogItem;
using ECMAMessageSeverity = ECMA2Yaml.MessageSeverity;
using MAML2YamlConverter = maml::MAML2Yaml.Lib.MAML2YamlConverter;
using MAMLLogItem = maml::MAML2Yaml.Lib.Logging.LogItem;
using MAMLMessageSeverity = maml::MAML2Yaml.Lib.Logging.MessageSeverity;

namespace Microsoft.Docs.Build;

internal class OpsPreProcessor
{
    private static readonly object s_lock = new();

    private readonly Config _config;
    private readonly BuildOptions _buildOptions;
    private readonly ErrorBuilder _errors;
    private readonly RepositoryProvider _repositoryProvider;

    public OpsPreProcessor(Config config, ErrorBuilder errors, BuildOptions buildOptions, RepositoryProvider repositoryProvider)
    {
        _config = config;
        _errors = errors;
        _buildOptions = buildOptions;
        _repositoryProvider = repositoryProvider;
    }

    public bool Run()
    {
        return PreProcessMonoDocXml() & PreProcessMAML() && PreProcessDotnet();
    }

    private bool PreProcessMonoDocXml()
    {
        var result = true;
        if (_config.Monodoc is null)
        {
            return result;
        }

        using (Progress.Start("Pre-process monodoc XML files"))
        {
            lock (s_lock)
            {
                for (var index = 0; index < _config.Monodoc.Length; index++)
                {
                    var monodocConfig = _config.Monodoc[index];
                    var xmlDirectory = Path.GetFullPath(Path.Combine(_buildOptions.DocsetPath, monodocConfig.SourceXmlFolder));

                    // skip monodoc config if source xml folder does not exist
                    if (!Directory.Exists(xmlDirectory))
                    {
                        continue;
                    }

                    Directory.CreateDirectory(monodocConfig.OutputYamlFolder);

                    var fallbackXmlPath = _buildOptions.FallbackDocsetPath is null
                        ? null
                        : Path.GetFullPath(Path.Combine(_buildOptions.FallbackDocsetPath.Value, monodocConfig.SourceXmlFolder));
                    var fallbackOutputDirectory = _buildOptions.FallbackDocsetPath is null
                        ? null
                        : Path.GetFullPath(Path.Combine(_buildOptions.DocsetPath, ".fallback", monodocConfig.OutputYamlFolder));
                    var (repository, _) = _repositoryProvider.GetRepository(new PathString(xmlDirectory));
                    result &= ECMA2YamlConverter.Run(
                        xmlDirectory,
                        outputDirectory: Path.GetFullPath(Path.Combine(_buildOptions.DocsetPath, monodocConfig.OutputYamlFolder)),
                        fallbackXmlDirectory: fallbackXmlPath,
                        fallbackOutputDirectory: fallbackOutputDirectory,
                        logWriter: LogError,
                        logContentBaseDirectory: _buildOptions.DocsetPath,
                        sourceMapFilePath: Path.Combine(_buildOptions.DocsetPath, $".sourcemap-ecma-{index}.json"),
                        publicGitRepoUrl: _config.EditRepositoryUrl ?? repository?.Url,
                        publicGitBranch: _config.EditRepositoryBranch ?? repository?.Branch,
                        config: monodocConfig);
                }
            }
        }
        return result;
    }

    private bool PreProcessMAML()
    {
        var result = true;
        if (_config.MAMLMonikerPath is null)
        {
            return result;
        }

        using (Progress.Start("Pre-process MAML markdown files"))
        {
            lock (s_lock)
            {
                for (var index = 0; index < _config.MAMLMonikerPath.Length; index++)
                {
                    var monikerMappingPath = _config.MAMLMonikerPath[index];

                    result &= MAML2YamlConverter.Run(
                        docsetPath: _buildOptions.DocsetPath,
                        monikerMappingPath: Path.GetFullPath(Path.Combine(_buildOptions.DocsetPath, monikerMappingPath)),
                        logWriter: LogError,
                        sourceMapFilePath: Path.Combine(_buildOptions.DocsetPath, $".sourcemap-maml-{index}.json"));
                }
            }
        }
        return result;
    }

    private bool PreProcessDotnet()
    {
        if (_config.Dotnet is null)
        {
            return true;
        }

        using (Progress.Start("Pre-process dotnet files"))
        {
            lock (s_lock)
            {
                var docfxConfig = new JObject { ["dotnet"] = _config.Dotnet };
                var env = new Dictionary<string, string> { ["DOCFX_CONFIG"] = docfxConfig.ToString() };
                var exe = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "docfx-api-dotnet.exe" : "docfx-api-dotnet");
                ProcessUtility.Execute(exe, "", cwd: _buildOptions.DocsetPath, stdout: false, env: env);
            }
        }
        return true;
    }

    private void LogError(ECMALogItem item)
    {
        if (!string.IsNullOrEmpty(item.Code))
        {
            var file = item.File is null
                ? null
                : item.File.StartsWith("_repo.en-us/") || item.File.StartsWith("_repo.en-us\\")
                    ? FilePath.Fallback(new PathString(item.File["_repo.en-us/".Length..]))
                    : FilePath.Content(new PathString(item.File));

            var source = file is null ? null : new SourceInfo(file, item.Line ?? 0, 0);

            _errors.Add(new Error(MapLevel(item.MessageSeverity), item.Code, $"{item.Message}", source));
        }

        static ErrorLevel MapLevel(ECMAMessageSeverity level) => level switch
        {
            ECMAMessageSeverity.Error => ErrorLevel.Error,
            ECMAMessageSeverity.Warning => ErrorLevel.Warning,
            ECMAMessageSeverity.Suggestion => ErrorLevel.Suggestion,
            ECMAMessageSeverity.Info => ErrorLevel.Info,
            _ => ErrorLevel.Off,
        };
    }

    private void LogError(MAMLLogItem item)
    {
        if (!string.IsNullOrEmpty(item.Code))
        {
            var file = item.File is null
                ? null
                : FilePath.Content(new PathString(item.File));

            var source = file is null ? null : new SourceInfo(file, item.Line ?? 0, 0);

            _errors.Add(new Error(MapLevel(item.MessageSeverity), item.Code, $"{item.Message}", source));
        }

        static ErrorLevel MapLevel(MAMLMessageSeverity level) => level switch
        {
            MAMLMessageSeverity.Error => ErrorLevel.Error,
            MAMLMessageSeverity.Warning => ErrorLevel.Warning,
            MAMLMessageSeverity.Suggestion => ErrorLevel.Suggestion,
            MAMLMessageSeverity.Info => ErrorLevel.Info,
            _ => ErrorLevel.Off,
        };
    }
}
