// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.MetadataMergers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
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
                MergePageViewModel(files, parameters, outputBase);
                MergeToc(files, outputBase);
                Logger.LogInfo("Merge metadata completed.");
            }
        }

        private static void MergePageViewModel(List<FileAndType> files, MetadataMergeParameters parameters, string outputBase)
        {
            var p = new ManagedReferenceDocumentProcessor();
            var host = new HostService(
                from f in files
                where f.Type == DocumentType.Article && !"toc.yml".Equals(Path.GetFileName(f.File), StringComparison.OrdinalIgnoreCase)
                select Load(p, parameters.Metadata, parameters.FileMetadata, f));
            // todo : temp reuse plugin
            var pv = new ApplyPlatformVersion();
            pv.Prebuild(host.Models, host);
            var core = new MergeManagedReferenceDocument();
            host.Reload(core.Prebuild(host.Models, host));
            foreach (var m in host.Models)
            {
                m.File = m.PathRewriter(m.File);
            }
            foreach (var m in host.Models)
            {
                YamlUtility.Serialize(Path.Combine(outputBase, m.File), m.Content);
            }
        }

        private static FileModel Load(
            IDocumentProcessor processor,
            ImmutableDictionary<string, object> metadata,
            FileMetadata fileMetadata,
            FileAndType file)
        {
            using (new LoggerFileScope(file.File))
            {
                Logger.LogVerbose($"Plug-in {processor.Name}: Loading...");

                var path = Path.Combine(file.BaseDir, file.File);
                metadata = ApplyFileMetadata(path, metadata, fileMetadata);
                return processor.Load(file, metadata);
            }
        }

        private static ImmutableDictionary<string, object> ApplyFileMetadata(
            string file,
            ImmutableDictionary<string, object> metadata,
            FileMetadata fileMetadata)
        {
            if (fileMetadata == null || fileMetadata.Count == 0) return metadata;
            var result = new Dictionary<string, object>(metadata);
            var baseDir = string.IsNullOrEmpty(fileMetadata.BaseDir) ? Environment.CurrentDirectory : fileMetadata.BaseDir;
            var relativePath = PathUtility.MakeRelativePath(baseDir, file);
            foreach (var item in fileMetadata)
            {
                // As the latter one overrides the former one, match the pattern from latter to former
                for (int i = item.Value.Length - 1; i >= 0; i--)
                {
                    if (item.Value[i].Glob.Match(relativePath))
                    {
                        // override global metadata if metadata is defined in file metadata
                        result[item.Value[i].Key] = item.Value[i].Value;
                        Logger.LogVerbose($"{relativePath} matches file metadata with glob pattern {item.Value[i].Glob.Raw} for property {item.Value[i].Key}");
                        break;
                    }
                }
            }
            return result.ToImmutableDictionary();
        }

        private static void MergeToc(List<FileAndType> files, string outputBase)
        {
            var tocFiles =
                (from f in files
                 where f.Type == DocumentType.Article && "toc.yml".Equals(Path.GetFileName(f.File), StringComparison.OrdinalIgnoreCase)
                 select f).ToList();
            var vm = MergeTocViewModel(
                from f in tocFiles
                select YamlUtility.Deserialize<TocViewModel>(Path.Combine(f.BaseDir, f.File)));
            YamlUtility.Serialize(Path.Combine(outputBase, tocFiles[0].PathRewriter(tocFiles[0].File)), vm);
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
                if (first.Items.Count == 0)
                {
                    first.Items = null;
                }
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
