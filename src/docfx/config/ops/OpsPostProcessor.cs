// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Docs.LearnValidation;

namespace Microsoft.Docs.Build;

internal class OpsPostProcessor
{
    private static readonly object s_lock = new();

    private readonly Config _config;
    private readonly BuildOptions _buildOptions;
    private readonly ErrorBuilder _errors;
    private readonly ILearnServiceAccessor _learnServiceAccessor;
    private readonly ExternalXref[] _externalXrefs;

    public OpsPostProcessor(
        Config config,
        ErrorBuilder errors,
        BuildOptions buildOptions,
        ILearnServiceAccessor learnServiceAccessor,
        ExternalXref[] externalXrefs)
    {
        _config = config;
        _errors = errors;
        _buildOptions = buildOptions;
        _learnServiceAccessor = learnServiceAccessor;
        _externalXrefs = externalXrefs;
    }

    public void Run()
    {
        PostProcessLearnValidation();
    }

    private void PostProcessLearnValidation()
    {
        if (!_config.IsLearn || _config.OutputType != OutputType.PageJson || _config.DryRun)
        {
            return;
        }

        using (Progress.Start("Postprocessing learn contents"))
        {
            lock (s_lock)
            {
                LearnValidationEntry.Run(
                    repoUrl: _buildOptions.Repository?.Url ?? "",
                    repoBranch: _buildOptions.Repository?.Branch ?? "",
                    docsetName: _config.Name,
                    docsetOutputPath: _buildOptions.OutputPath,
                    publishFilePath: Path.GetFullPath(Path.Combine(_buildOptions.OutputPath, ".publish.json")),
                    manifestFilePath: Path.GetFullPath(Path.Combine(_buildOptions.OutputPath, _config.BasePath, ".manifest.json")),
                    isLocalizationBuild: _buildOptions.IsLocalizedBuild,
                    writeLog: LogError,
                    noDrySync: _config.NoDrySync,
                    isSharedItem: IsSharedItem,
                    learnServiceAccessor: _learnServiceAccessor);
            }
        }
    }

    private void LogError(LearnLogItem item)
    {
        var source = item.File is null ? null : new SourceInfo(new FilePath(item.File));
        _errors.Add(new Error(MapLevel(item.ErrorLevel), item.ErrorCode.ToString(), $"{item.Message}", source));

        static ErrorLevel MapLevel(LearnErrorLevel level) => level switch
        {
            LearnErrorLevel.Error => ErrorLevel.Error,
            LearnErrorLevel.Warning => ErrorLevel.Warning,
            _ => ErrorLevel.Off,
        };
    }

    private bool IsSharedItem(string uid, string schemaType)
    {
        return _externalXrefs.Any(xref => xref.Uid == uid && schemaType == xref.SchemaType);
    }
}
