// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Build
    {
        public static async Task Run(string docsetPath, CommandLineOptions options, Report report)
        {
            var config = Config.Load(docsetPath, options);

            report.Configure(docsetPath, config);

            var outputPath = Path.Combine(docsetPath, config.Output.Path);
            var context = new Context(report, outputPath);
            var docset = new Docset(context, docsetPath, config, options);

            var tocMap = await BuildTableOfContents.BuildTocMap(context, docset.BuildScope);
            var contribution = ContributionInfo.Load(docset);

            var (files, sourceDependencies) = await BuildFiles(context, docset.BuildScope, tocMap, contribution);

            BuildManifest.Build(context, files, sourceDependencies);

            if (options.Legacy)
            {
                Legacy.ConvertToLegacyModel(docset, context, files, sourceDependencies, tocMap);
            }
        }

        private static async Task<(List<Document> files, DependencyMap sourceDependencies)> BuildFiles(
            Context context,
            HashSet<Document> buildScope,
            TableOfContentsMap tocMap,
            ContributionInfo contribution)
        {
            using (Progress.Start("Building files"))
            {
                var sourceDependencies = new ConcurrentDictionary<Document, List<DependencyItem>>();
                var filesBuilder = new DocumentListBuilder();
                var filesWithErrors = new ConcurrentBag<Document>();

                await ParallelUtility.ForEach(buildScope, BuildOneFile, ShouldBuildFile, Progress.Update);

                var files = filesBuilder.Build(context).OrderBy(file => file.FilePath).Except(filesWithErrors).ToList();
                var allDependencies = sourceDependencies.OrderBy(d => d.Key.FilePath).ToDictionary(k => k.Key, v => v.Value);

                return (files, new DependencyMap(allDependencies));

                async Task BuildOneFile(Document file, Action<Document> buildChild)
                {
                    var (hasError, dependencyMap) = await BuildFile(context, file, tocMap, contribution, buildChild);
                    if (hasError)
                    {
                        filesWithErrors.Add(file);
                    }
                    foreach (var (source, dependencies) in dependencyMap)
                    {
                        sourceDependencies.TryAdd(source, dependencies);
                    }
                }

                bool ShouldBuildFile(Document file)
                {
                    return file.ContentType != ContentType.Unknown && filesBuilder.TryAdd(file);
                }
            }
        }

        private static async Task<(bool hasError, DependencyMap)> BuildFile(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            ContributionInfo contribution,
            Action<Document> buildChild)
        {
            try
            {
                var model = (object)null;
                var dependencies = DependencyMap.Empty;
                var errors = Enumerable.Empty<Error>();

                switch (file.ContentType)
                {
                    case ContentType.Resource:
                        BuildResource(context, file);
                        return (false, DependencyMap.Empty);
                    case ContentType.Page:
                        (errors, model, dependencies) = await BuildPage.Build(file, tocMap, contribution, buildChild);
                        break;
                    case ContentType.TableOfContents:
                        (errors, model, dependencies) = BuildTableOfContents.Build(file, tocMap, buildChild);
                        break;
                    case ContentType.Redirection:
                        model = BuildRedirection(file);
                        break;
                }

                var hasErrors = context.Report(file.ToString(), errors);
                if (model != null && !hasErrors)
                {
                    context.WriteJson(model, file.OutputPath);
                    return (false, dependencies);
                }

                return (true, dependencies);
            }
            catch (DocfxException ex)
            {
                context.Report(file.ToString(), ex.Error);
                return (true, DependencyMap.Empty);
            }
        }

        private static void BuildResource(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Resource);

            if (file.Docset.Config.Output.CopyResources)
            {
                context.Copy(file, file.OutputPath);
            }
        }

        private static PageModel BuildRedirection(Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);

            return new PageModel
            {
                RedirectionUrl = file.RedirectionUrl,
                Locale = file.Docset.Config.Locale,
                Id = file.Id.id,
                VersionIndependentId = file.Id.versionIndependentId,
                Metadata = Metadata.GetFromConfig(file),
            };
        }
    }
}
