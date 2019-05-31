// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class BuildScope
    {
        public Func<string, bool> Glob { get; private set; }

        public HashSet<Document> Files { get; private set; }

        public HashSet<Document> FilesWithFallback { get; private set; }

        public RedirectionMap Redirections { get; private set; }

        public static (List<Error>, BuildScope) Create(Docset docset)
        {
            var glob = CreateGlob(docset);

            var (errors, redirections) = RedirectionMap.Create(docset, glob);
            var files = GetFiles(docset, glob).Concat(redirections.Files).ToHashSet();
            var filesWithFallback = files;

            if (docset.FallbackDocset != null)
            {
                filesWithFallback = files.Concat(
                    GetFiles(docset.FallbackDocset, CreateGlob(docset.FallbackDocset))).ToHashSet();
            }

            var result = new BuildScope
            {
                Glob = glob,
                Files = files,
                FilesWithFallback = filesWithFallback,
                Redirections = redirections,
            };

            return (errors, result);
        }

        private static Func<string, bool> CreateGlob(Docset docset)
        {
            return GlobUtility.CreateGlobMatcher(
                  docset.Config.Files,
                  docset.Config.Exclude.Concat(Config.DefaultExclude).ToArray());
        }

        private static IReadOnlyList<Document> GetFiles(Docset docset, Func<string, bool> glob)
        {
            using (Progress.Start("Globbing files"))
            {
                var files = new ListBuilder<Document>();

                ParallelUtility.ForEach(
                    Directory.EnumerateFiles(docset.DocsetPath, "*.*", SearchOption.AllDirectories),
                    file =>
                    {
                        var relativePath = Path.GetRelativePath(docset.DocsetPath, file);
                        if (glob(relativePath))
                        {
                            files.Add(Document.CreateFromFile(docset, relativePath));
                        }
                    });

                return files.ToList();
            }
        }
    }
}
