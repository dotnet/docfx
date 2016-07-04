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

        bool HasMetadataValidation { get; }
        void ValidateInputMetadata(string file, ImmutableDictionary<string, object> metadata);

        #region Log
        void LogVerbose(string message, string file = null, string line = null);
        void LogInfo(string message, string file = null, string line = null);
        void LogWarning(string message, string file = null, string line = null);
        void LogError(string message, string file = null, string line = null);
        #endregion
    }
}
