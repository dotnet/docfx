// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Docfx.Common;
using Docfx.MarkdigEngine;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

[Export(typeof(IHostService))]
class HostService : IHostService
{
    private Dictionary<string, List<FileModel>> _uidIndex = [];

    public TemplateProcessor Template { get; set; }

    public ImmutableList<FileModel> Models { get; private set; }

    public ImmutableDictionary<string, FileAndType> SourceFiles { get; set; }

    public ImmutableList<string> InvalidSourceFiles { get; set; }

    public IMarkdownService MarkdownService { get; set; }

    public ImmutableList<IInputMetadataValidator> Validators { get; set; }

    public ImmutableList<TreeItemRestructure> TableOfContentRestructions { get; set; }

    public string VersionName { get; }

    public string VersionOutputFolder { get; }

    public GroupInfo GroupInfo { get; }

    public HostService(IEnumerable<FileModel> models, string versionName = null, string versionDir = null, GroupInfo groupInfo = null)
    {
        VersionName = versionName;
        VersionOutputFolder = versionDir;
        GroupInfo = groupInfo;

        Reload(models);
    }

    public IDocumentProcessor Processor { get; set; }

    public ImmutableList<FileModel> GetModels(DocumentType? type)
    {
        if (type == null)
        {
            return Models;
        }
        return (from m in Models where m.Type == type select m).ToImmutableList();
    }

    public ImmutableHashSet<string> GetAllUids()
    {
        return _uidIndex.Keys.ToImmutableHashSet();
    }

    public ImmutableList<FileModel> LookupByUid(string uid)
    {
        if (_uidIndex.TryGetValue(uid, out List<FileModel> result))
        {
            return result.ToImmutableList();
        }
        return [];
    }

    public MarkupResult Markup(string markdown, FileAndType ft)
    {
        return Markup(markdown, ft, false);
    }

    public MarkupResult Markup(string markdown, FileAndType ft, bool omitParse)
    {
        return MarkupCore(markdown, ft, omitParse);
    }

    public MarkupResult Parse(MarkupResult markupResult, FileAndType ft)
    {
        return MarkupUtility.Parse(markupResult, ft.File, SourceFiles);
    }

    private MarkupResult MarkupCore(string markdown, FileAndType ft, bool omitParse)
    {
        try
        {
            var mr = MarkdownService is MarkdigMarkdownService markdig
                ? markdig.Markup(markdown, ft.File, ft.Type is DocumentType.Overwrite)
                : MarkdownService.Markup(markdown, ft.File);
            if (omitParse)
            {
                return mr;
            }
            return Parse(mr, ft);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Fail("Markup failed!");
            var message = $"Markup failed: {ex.Message}.";
            Logger.LogError(message, code: ErrorCodes.Build.InvalidMarkdown);
            throw new DocumentException(message, ex);
        }
    }

    public bool HasMetadataValidation => Validators.Count > 0;

    public string MarkdownServiceName => MarkdownService.Name;

    public void ValidateInputMetadata(string sourceFile, ImmutableDictionary<string, object> metadata)
    {
        foreach (var v in Validators)
        {
            lock (v)
            {
                v.Validate(sourceFile, metadata);
            }
        }
    }

    public void LogDiagnostic(string message, string file, string line)
    {
        Logger.LogDiagnostic(message, file: file, line: line);
    }

    public void LogVerbose(string message, string file, string line)
    {
        Logger.LogVerbose(message, file: file, line: line);
    }

    public void LogInfo(string message, string file, string line)
    {
        Logger.LogInfo(message, file: file, line: line);
    }

    public void LogSuggestion(string message, string file, string line)
    {
        Logger.LogSuggestion(message, file: file, line: line);
    }

    public void LogWarning(string message, string file, string line)
    {
        Logger.LogWarning(message, file: file, line: line);
    }

    public void LogError(string message, string file, string line)
    {
        Logger.LogError(message, file: file, line: line);
    }

    public void Reload(IEnumerable<FileModel> models)
    {
        Models = models.ToImmutableList();

        _uidIndex = (
            from m in models
            from uid in m.Uids.Select(s => s.Name).Distinct()
            group m by uid).ToDictionary(g => g.Key, g => g.ToList());
    }
}
