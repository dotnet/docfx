// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;

    public class BuildOverwriteDocument : BaseDocumentBuildStep
    {
        private readonly IModelAttributeHandler _defaultHandler =
            new CompositeModelAttributeHandler(
                new UniqueIdentityReferenceHandler(),
                new MarkdownContentHandler()
            );

        public override string Name => nameof(BuildOverwriteDocument);

        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            switch (model.Type)
            {
                case DocumentType.Overwrite:
                    BuildOverwrite(host, model);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void BuildOverwrite(IHostService host, FileModel model)
        {
            if (!(model.Content is IEnumerable<OverwriteDocumentModel> overwrites))
            {
                overwrites = MarkdownReader.ReadMarkdownAsOverwrite(host, model.FileAndType).ToList();
                model.Content = overwrites;
            }

            model.LinkToFiles = overwrites.SelectMany(o => o.LinkToFiles).ToImmutableHashSet();
            model.LinkToUids = overwrites.SelectMany(o => o.LinkToUids).ToImmutableHashSet();
            model.FileLinkSources = overwrites.SelectMany(o => o.FileLinkSources).GroupBy(i => i.Key, i => i.Value).ToImmutableDictionary(i => i.Key, i => i.SelectMany(l => l).ToImmutableList());
            model.UidLinkSources = overwrites.SelectMany(o => o.UidLinkSources).GroupBy(i => i.Key, i => i.Value).ToImmutableDictionary(i => i.Key, i => i.SelectMany(l => l).ToImmutableList());
            model.Uids = (from item in overwrites
                          where !string.IsNullOrEmpty(item.Uid)
                          select new UidDefinition(
                              item.Uid,
                              model.LocalPathFromRoot,
                              item.Documentation.StartLine + 1)).ToImmutableArray();
            foreach (var d in overwrites.SelectMany(s => s.Dependency))
            {
                host.ReportDependencyTo(model, d, DependencyTypeName.Include);
            }
        }
    }
}
