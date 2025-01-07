// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.MarkdigEngine;
using Docfx.Plugins;

namespace Docfx.Build.SchemaDriven.Processors;

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
        Uids = [];
        UidLinkSources = [];
        FileLinkSources = [];
        Dependency = [];
        XRefSpecs = [];
        ExternalXRefSpecs = [];
        Metadata = new Dictionary<string, object>();
        if (((IDictionary<string, object>)fm.Properties).TryGetValue("PathProperties", out var properties))
        {
            var pathProperties = properties as Dictionary<string, Dictionary<string, object>>;
            PathProperties = pathProperties ?? throw new ArgumentException($"PathProperties is expecting a dictionary however is a {pathProperties.GetType()}");
        }
        else
        {
            fm.Properties.PathProperties = PathProperties = [];
        }

        Host = hs;
        BuildContext = bc;
        MarkdigMarkdownService = markdigMarkdownService;
    }
}
