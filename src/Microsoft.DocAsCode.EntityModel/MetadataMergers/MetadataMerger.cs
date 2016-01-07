// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.MetadataMergers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.EntityModel.Plugins;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class MetadataMerger
    {
        public const string PhaseName = "Merge Metadata";

        public void Merge(MetadataMergeParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (parameters.OutputBaseDir == null)
            {
                throw new ArgumentException("Output folder cannot be null.", nameof(parameters) + "." + nameof(parameters.OutputBaseDir));
            }
            if (parameters.Files == null)
            {
                throw new ArgumentException("Source files cannot be null.", nameof(parameters) + "." + nameof(parameters.Files));
            }
            if (parameters.Metadata == null)
            {
                parameters.Metadata = ImmutableDictionary<string, object>.Empty;
            }

            using (new LoggerPhaseScope(PhaseName))
            {
                Directory.CreateDirectory(parameters.OutputBaseDir);
                var files = parameters.Files.EnumerateFiles().ToList();
                var outputBase = Path.Combine(Environment.CurrentDirectory, parameters.OutputBaseDir);
                Logger.LogInfo("Start merge metadata...");
                MergePageViewModel(files, outputBase);
                MergeToc(files, outputBase);
                Logger.LogInfo("Merge metadata completed.");
            }
        }

        private static void MergePageViewModel(List<FileAndType> files, string outputBase)
        {
            var host = new HostService(
                from f in files
                where f.Type == DocumentType.Article && !"toc.yml".Equals(Path.GetFileName(f.File), StringComparison.OrdinalIgnoreCase)
                let vm = YamlUtility.Deserialize<PageViewModel>(Path.Combine(f.BaseDir, f.File))
                select new FileModel(f, vm)
                {
                    Uids = (from item in vm.Items select item.Uid).ToImmutableArray(),
                });
            // todo : temp reuse plugin
            var pv = new ApplyPlatformVersion();
            pv.Prebuild(host.Models, host);
            var core = new MergeManagedReferenceDocument();
            host.Reload(core.Postbuild(host.Models, host));
            foreach (var m in host.Models)
            {
                YamlUtility.Serialize(Path.Combine(outputBase, m.File), m.Content);
            }
        }

        private static void MergeToc(List<FileAndType> files, string outputBase)
        {
            var tocModels =
                (from f in files
                 where f.Type == DocumentType.Article && "toc.yml".Equals(Path.GetFileName(f.File), StringComparison.OrdinalIgnoreCase)
                 select YamlUtility.Deserialize<TocViewModel>(Path.Combine(f.BaseDir, f.File))).ToList();
            var vm = MergeTocViewModel(tocModels);
            YamlUtility.Serialize(Path.Combine(outputBase, "toc.yml"), vm);
        }

        private static TocItemViewModel MergeTocItem(List<TocItemViewModel> items)
        {
            var first = items[0];
            if (items.Count > 1)
            {
                first.Items = MergeTocViewModel(
                    from item in items
                    where item.Items?.Count > 0
                    select item.Items);
            }
            return first;
        }

        private static TocViewModel MergeTocViewModel(IEnumerable<TocViewModel> items)
        {
            return new TocViewModel(
                (from item in items
                 where item.Count > 0
                 select item).ToList()
                .Merge(TocComparer.Instance, MergeTocItem)
                .OrderBy(x => x.Name));
        }

        private sealed class TocComparer
            : Comparer<TocItemViewModel>
        {
            public static readonly TocComparer Instance = new TocComparer();

            public override int Compare(TocItemViewModel x, TocItemViewModel y)
            {
                return string.Compare(x.Uid, y.Uid);
            }
        }
    }
}
