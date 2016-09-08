// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Immutable;

    public interface IHostService
    {
        [Obsolete]
        string MarkupToHtml(string markdown, string file);
        [Obsolete]
        MarkupResult ParseHtml(string html, FileAndType ft);
        MarkupResult Parse(MarkupResult markupResult, FileAndType ft);
        MarkupResult Markup(string markdown, FileAndType ft);
        MarkupResult Markup(string markdown, FileAndType ft, bool omitParse);
        ImmutableDictionary<string, FileAndType> SourceFiles { get; }
        ImmutableHashSet<string> GetAllUids();
        ImmutableList<FileModel> GetModels(DocumentType? type = null);
        ImmutableList<FileModel> LookupByUid(string uid);

        /// <summary>
        /// report dependency
        /// </summary>
        /// <param name="from">'from' node's file path from working directory</param>
        /// <param name="to">'to' node's file path from working directory</param>
        /// <param name="reportedBy">'reportedby' node's file path from working directory</param>
        /// <param name="type">dependency type</param>
        void ReportDependency(string from, string to, string reportedBy, string type);

        /// <summary>
        /// report dependency to
        /// </summary>
        /// <param name="currentFileModel">filemodel of 'from' node</param>
        /// <param name="to">'to' node's file path from working directory or file path relative to 'from' filemodel</param>
        /// <param name="type">dependency type</param>
        void ReportDependencyTo(FileModel currentFileModel, string to, string type);

        /// <summary>
        /// report dependency from
        /// </summary>
        /// <param name="currentFileModel">filemodel of 'to' node</param>
        /// <param name="from">'from' node's file path from working directory or file path relative to 'to' filemodel</param>
        /// <param name="type">dependency type</param>
        void ReportDependencyFrom(FileModel currentFileModel, string from, string type);

        void RegisterDependencyType(string name, bool isTransitive, bool triggerBuild);

        /// <summary>
        /// Get current <see cref="IDocumentProcessor"/>.
        /// </summary>
        IDocumentProcessor Processor { get; }

        bool HasMetadataValidation { get; }
        void ValidateInputMetadata(string file, ImmutableDictionary<string, object> metadata);

        #region Log
        void LogDiagnostic(string message, string file = null, string line = null);
        void LogVerbose(string message, string file = null, string line = null);
        void LogInfo(string message, string file = null, string line = null);
        void LogWarning(string message, string file = null, string line = null);
        void LogError(string message, string file = null, string line = null);
        #endregion
    }
}
