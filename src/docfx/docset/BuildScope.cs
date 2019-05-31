// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class BuildScope
    {
        public HashSet<Document> Files { get; }

        /// <summary>
        /// Gets the scan scope used to generate toc map, xref map, xxx map before build
        /// </summary>
        public HashSet<Document> ScanFiles { get; }

        /*
        private HashSet<Document> CreateBuildScope(IEnumerable<Document> redirections, Func<string, bool> glob)
        {
            using (Progress.Start("Globbing files"))
            {
                var files = new ListBuilder<Document>();

                ParallelUtility.ForEach(
                    Directory.EnumerateFiles(DocsetPath, "*.*", SearchOption.AllDirectories),
                    file =>
                    {
                        var relativePath = Path.GetRelativePath(DocsetPath, file);
                        if (glob(relativePath))
                        {
                            files.Add(Document.CreateFromFile(this, relativePath));
                        }
                    });

                return new HashSet<Document>(files.ToList().Concat(redirections));
            }
        }

        private static HashSet<Document> GetScanScope(Docset docset)
        {
            var scanScopeFilePaths = new HashSet<string>(PathUtility.PathComparer);
            var scanScope = new HashSet<Document>();

            foreach (var buildScope in new[] { docset.LocalizationDocset?.BuildScope, docset.BuildScope, docset.FallbackDocset?.BuildScope })
            {
                if (buildScope is null)
                {
                    continue;
                }

                foreach (var document in buildScope)
                {
                    if (scanScopeFilePaths.Add(document.FilePath))
                    {
                        scanScope.Add(document);
                    }
                }
            }

            return scanScope;
        }*/
    }
}
