// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Immutable;

    public interface IHostService
    {
        IBuildParameters BuildParameters { get; }

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
        MarkupResult Markup(string markdown, FileAndType ft, bool omitParse, bool enableValidation);
        ImmutableDictionary<string, FileAndType> SourceFiles { get; }
        ImmutableDictionary<string, FileIncrementalInfo> IncrementalInfos { get; }
        ImmutableHashSet<string> GetAllUids();
        ImmutableList<FileModel> GetModels(DocumentType? type = null);
        ImmutableList<FileModel> LookupByUid(string uid);

        /// <summary>
        /// report dependency to
        /// </summary>
        /// <param name="currentFileModel">filemodel of 'from' node</param>
        /// <param name="to">'to' node's file path from working directory or file path relative to 'from' filemodel</param>
        /// <param name="type">dependency type</param>
        void ReportDependencyTo(FileModel currentFileModel, string to, string type);

        /// <summary>
        /// report dependency to
        /// </summary>
        /// <param name="currentFileModel">filemodel of 'from' node</param>
        /// <param name="to">'to' node's value</param>
        /// <param name="toType">'to' node's type, it could be `file` or reference type</param>
        /// <param name="type">dependency type</param>
        void ReportDependencyTo(FileModel currentFileModel, string to, string toType, string type);

        /// <summary>
        /// report dependency from
        /// </summary>
        /// <param name="currentFileModel">filemodel of 'to' node</param>
        /// <param name="from">'from' node's file path from working directory or file path relative to 'to' filemodel</param>
        /// <param name="type">dependency type</param>
        void ReportDependencyFrom(FileModel currentFileModel, string from, string type);

        /// <summary>
        /// report dependency from
        /// </summary>
        /// <param name="currentFileModel">filemodel of 'to' node</param>
        /// <param name="from">'from' node's value</param>
        /// <param name="fromType">'from' node's type, it could be `file` or reference type</param>
        /// <param name="type">dependency type</param>
        void ReportDependencyFrom(FileModel currentFileModel, string from, string fromType, string type);

        /// <summary>
        /// report reference
        /// </summary>
        /// <param name="currentFileModel">filemodel</param>
        /// <param name="reference">the reference that the 'filemodel' could provide</param>
        /// <param name="referenceType">the type of the reference</param>
        void ReportReference(FileModel currentFileModel, string reference, string referenceType);

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
}
