// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility.EntityMergers;

    [Export(nameof(RestApiDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverrideDocumentForRestApi : BaseDocumentBuildStep
    {
        private readonly MergerFacade Merger = new MergerFacade(
                new DictionaryMerger(
                    new KeyedListMerger(
                        new ReflectionEntityMerger())));

        public override string Name => nameof(ApplyOverrideDocumentForRestApi);

        public override int BuildOrder => 0x10;

        public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            if (models.Count > 0)
            {
                ApplyOverrides(models, host);
            }
        }

        #region Private methods

        /// <summary>
        /// TODO: extract to base class together with class ApplyOverrideDocument
        /// </summary>
        /// <param name="models"></param>
        /// <param name="host"></param>
        private void ApplyOverrides(ImmutableList<FileModel> models, IHostService host)
        {
            foreach (var uid in host.GetAllUids())
            {
                var ms = host.LookupByUid(uid);
                var od = ms.Where(m => m.Type == DocumentType.Override).ToList();
                var articles = ms.Except(od).ToList();
                if (articles.Count == 0 || od.Count == 0)
                {
                    continue;
                }

                if (od.Count > 1)
                {
                    var uidDefinitions = od[0].Uids.Where(u => u.Name == uid);
                    var errorMessage = string.Join(",", uidDefinitions.Select(s => $"\"{s.File}\" Line {s.Line}"));
                    throw new DocumentException($"UID \"{uid}\" is defined in multiple places: {errorMessage}. Only one overwrite document is allowed per particular UID.");
                }

                var ovm = ((List<RestApiItemViewModel>)od[0].Content).Single(vm => vm.Uid == uid);
                foreach (
                    var pair in
                        from model in articles
                        from item in new RestApiItemViewModel[] { (RestApiItemViewModel)model.Content }.Concat(((RestApiItemViewModel)model.Content).Children)
                        where item.Uid == uid
                        select new { model, item })
                {
                    var vm = pair.item;
                    Merger.Merge(ref vm, ovm);
                    ((HashSet<string>)pair.model.Properties.LinkToUids).UnionWith((HashSet<string>)od[0].Properties.LinkToUids);
                    ((HashSet<string>)pair.model.Properties.LinkToFiles).UnionWith((HashSet<string>)od[0].Properties.LinkToFiles);
                }
            }
        }
        #endregion
    }
}
