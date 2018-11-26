// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdigEngine;
    using Microsoft.DocAsCode.Plugins;

    public class ProcessContext : IProcessContext
    {
        private readonly FileModel _model;

        public IHostService Host { get; }

        public IDocumentBuildContext BuildContext { get; }

        public List<UidDefinition> Uids { get; }

        public Dictionary<string, List<LinkSourceInfo>> UidLinkSources { get; }

        public Dictionary<string, List<LinkSourceInfo>> FileLinkSources { get; }

        public HashSet<string> Dependency { get; }

        public List<XRefSpec> XRefSpecs { get; }

        public List<XRefSpec> ExternalXRefSpecs { get; }

        public FileAndType OriginalFileAndType { get; }

        public FileAndType FileAndType { get; }

        public Dictionary<string, Dictionary<string, object>> PathProperties { get; }

        public IContentAnchorParser ContentAnchorParser { get; set; }

        public MarkdigMarkdownService MarkdigMarkdownService { get; set; }

        public IDictionary<string, object> Metadata { get; }

        public T GetModel<T>()
        {
            return (T)_model.Content;
        }

        public ProcessContext(IHostService hs, FileModel fm, IDocumentBuildContext bc = null)
            : this(hs, fm, bc, null) { }

        public ProcessContext(IHostService hs, FileModel fm, IDocumentBuildContext bc, MarkdigMarkdownService markdigMarkdownService = null)
        {
            _model = fm;
            OriginalFileAndType = fm.OriginalFileAndType;
            FileAndType = fm.FileAndType;
            Uids = new List<UidDefinition>();
            UidLinkSources = new Dictionary<string, List<LinkSourceInfo>>();
            FileLinkSources = new Dictionary<string, List<LinkSourceInfo>>();
            Dependency = new HashSet<string>();
            XRefSpecs = new List<XRefSpec>();
            ExternalXRefSpecs = new List<XRefSpec>();
            Metadata = new Dictionary<string, object>();
            if (((IDictionary<string, object>)(fm.Properties)).TryGetValue("PathProperties", out var properties))
            {
                var pathProperties = properties as Dictionary<string, Dictionary<string, object>>;
                PathProperties = pathProperties ?? throw new ArgumentException($"PathProperties is expecting a dictionary however is a {pathProperties.GetType()}");
            }
            else
            {
                fm.Properties.PathProperties = PathProperties = new Dictionary<string, Dictionary<string, object>>();
            }

            Host = hs;
            BuildContext = bc;
            MarkdigMarkdownService = markdigMarkdownService;
        }
    }
}
