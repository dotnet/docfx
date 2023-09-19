// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Docfx.Plugins;

public interface IHostService
{
    ImmutableList<TreeItemRestructure> TableOfContentRestructions { get; set; }

    /// <summary>
    /// current version's name, String.Empty for default version
    /// </summary>
    string VersionName { get; }

    /// <summary>
    /// current version's output base folder
    /// </summary>
    string VersionOutputFolder { get; }

    GroupInfo GroupInfo { get; }

    MarkupResult Parse(MarkupResult markupResult, FileAndType ft);
    MarkupResult Markup(string markdown, FileAndType ft);
    MarkupResult Markup(string markdown, FileAndType ft, bool omitParse);
    ImmutableDictionary<string, FileAndType> SourceFiles { get; }
    ImmutableHashSet<string> GetAllUids();
    ImmutableList<FileModel> GetModels(DocumentType? type = null);
    ImmutableList<FileModel> LookupByUid(string uid);

    /// <summary>
    /// Get current <see cref="IDocumentProcessor"/>.
    /// </summary>
    IDocumentProcessor Processor { get; }

    bool HasMetadataValidation { get; }
    void ValidateInputMetadata(string file, ImmutableDictionary<string, object> metadata);

    string MarkdownServiceName { get; }

    #region Log
    void LogDiagnostic(string message, string file = null, string line = null);
    void LogVerbose(string message, string file = null, string line = null);
    void LogInfo(string message, string file = null, string line = null);
    void LogSuggestion(string message, string file = null, string line = null);
    void LogWarning(string message, string file = null, string line = null);
    void LogError(string message, string file = null, string line = null);
    #endregion
}
