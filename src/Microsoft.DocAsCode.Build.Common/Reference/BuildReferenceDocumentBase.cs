// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;

    /// <summary>
    /// Base document build step for reference.
    /// </summary>
    public abstract class BuildReferenceDocumentBase : BaseDocumentBuildStep
    {
        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            switch (model.Type)
            {
                case DocumentType.Article:
                    BuildArticle(host, model);
                    break;
                case DocumentType.Overwrite:
                    BuildOverwrite(host, model);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected abstract void BuildArticle(IHostService host, FileModel model);

        protected virtual void BuildOverwrite(IHostService host, FileModel model)
        {
            var overwrites = MarkdownReader.ReadMarkdownAsOverwrite(host, model.FileAndType).ToList();
            model.Content = overwrites;
            model.LinkToFiles = overwrites.SelectMany(o => o.LinkToFiles).ToImmutableHashSet();
            model.LinkToUids = overwrites.SelectMany(o => o.LinkToUids).ToImmutableHashSet();
            model.Uids = (from item in overwrites
                          select new UidDefinition(
                              item.Uid,
                              model.LocalPathFromRoot,
                              item.Documentation.StartLine + 1)).ToImmutableArray();
        }
    }
}
