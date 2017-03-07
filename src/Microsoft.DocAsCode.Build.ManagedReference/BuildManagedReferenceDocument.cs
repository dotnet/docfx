// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.IO;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildManagedReferenceDocument : BuildReferenceDocumentBase, ISupportIncrementalBuildStep
    {
        private readonly IModelAttributeHandler _handler =
            new CompositeModelAttributeHandler(
                new UniqueIdentityReferenceHandler(),
                new MarkdownContentHandler()
                );

        public override string Name => nameof(BuildManagedReferenceDocument);

        #region BuildReferenceDocumentBase

        /// <summary>
        /// TODO: move to Base and share with other DocumentBuilder
        /// </summary>
        /// <param name="host"></param>
        /// <param name="model"></param>
        protected override void BuildArticle(IHostService host, FileModel model)
        {
            var pageViewModel = (PageViewModel)model.Content;
            var context = new HandleModelAttributesContext
            {
                EnableContentPlaceholder = false,
                Host = host,
                FileAndType = model.OriginalFileAndType,
                SkipMarkup = pageViewModel?.ShouldSkipMarkup ?? false,
            };

            _handler.Handle(pageViewModel, context);

            model.LinkToUids = model.LinkToUids.Union(context.LinkToUids);
            model.LinkToFiles = model.LinkToFiles.Union(context.LinkToFiles);
            model.FileLinkSources = model.FileLinkSources.ToDictionary(v => v.Key, v => v.Value.ToList())
                .Merge(context.FileLinkSources.Select(i => new KeyValuePair<string, IEnumerable<LinkSourceInfo>>(i.Key, i.Value)))
                .ToImmutableDictionary(v => v.Key, v => v.Value.ToImmutableList());
            model.UidLinkSources = model.UidLinkSources.ToDictionary(v => v.Key, v => v.Value.ToList())
                .Merge(context.UidLinkSources.Select(i => new KeyValuePair<string, IEnumerable<LinkSourceInfo>>(i.Key, i.Value)))
                .ToImmutableDictionary(v => v.Key, v => v.Value.ToImmutableList());
            foreach (var r in pageViewModel.References)
            {
                if (r.IsExternal == false)
                {
                    host.ReportDependencyTo(model, r.Uid, DependencyItemSourceType.Uid, DependencyTypeName.Reference);
                }
            }
        }

        #endregion

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => new[]
        {
            new DependencyType()
            {
                Name = DependencyTypeName.Reference,
                IsTransitive = false,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.None,
            }
        };

        #endregion
    }
}
