// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdigEngine;
    using Microsoft.DocAsCode.Plugins;

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
}
