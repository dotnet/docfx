// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.MarkdigEngine;
using Docfx.Plugins;

namespace Docfx.Build.SchemaDriven.Processors;

public interface IProcessContext
{
    IHostService Host { get; }

    IDocumentBuildContext BuildContext { get; }

    IContentAnchorParser ContentAnchorParser { get; }

    List<UidDefinition> Uids { get; }

    Dictionary<string, List<LinkSourceInfo>> UidLinkSources { get; }

    Dictionary<string, List<LinkSourceInfo>> FileLinkSources { get; }

    HashSet<string> Dependency { get; }

    List<XRefSpec> XRefSpecs { get; }

    List<XRefSpec> ExternalXRefSpecs { get; }

    FileAndType OriginalFileAndType { get; }

    FileAndType FileAndType { get; }

    Dictionary<string, Dictionary<string, object>> PathProperties { get; }

    MarkdigMarkdownService MarkdigMarkdownService { get; }

    T GetModel<T>();

    IDictionary<string, object> Metadata { get; }
}
