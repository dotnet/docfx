// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    [Serializable]
    public class TemplateManager
    {
        private readonly List<string> _templates = new List<string>();
        private readonly List<string> _themes = new List<string>();
        private readonly ResourceFinder _finder;

        public TemplateManager(Assembly assembly, string rootNamespace, List<string> templates, List<string> themes, string baseDirectory)
        {
            _finder = new ResourceFinder(assembly, rootNamespace, baseDirectory);
            _templates = templates;
            _themes = themes;
        }

        public bool TryExportTemplateFiles(string outputDirectory, string regexFilter = null)
        {
            return TryExportResourceFiles(_templates, outputDirectory, true, regexFilter);
        }

        public TemplateProcessor GetTemplateProcessor(DocumentBuildContext context, int maxParallelism)
        {
            return new TemplateProcessor(new CompositeResourceCollectionWithOverridden(_templates.Select(s => _finder.Find(s)).Where(s => s != null)), context, maxParallelism);
        }

        public void ProcessTheme(string outputDirectory, bool overwrite)
        {
            using (new LoggerPhaseScope("Apply Theme"))
            {
                if (_themes != null && _themes.Count > 0)
                {
                    TryExportResourceFiles(_themes, outputDirectory, overwrite);
                }

                Logger.LogInfo("Theme is applied.");
            }
        }

        private bool TryExportResourceFiles(IEnumerable<string> resourceNames, string outputDirectory, bool overwrite, string regexFilter = null)
        {
            if (string.IsNullOrEmpty(outputDirectory)) throw new ArgumentNullException(nameof(outputDirectory));
            if (!resourceNames.Any()) return false;
            bool isEmpty = true;
            using (var templateResource = new CompositeResourceCollectionWithOverridden(resourceNames.Select(s => _finder.Find(s)).Where(s => s != null)))
            {
                if (templateResource.IsEmpty)
                {
                    Logger.Log(LogLevel.Warning, $"No resource found for [{resourceNames.ToDelimitedString()}].");
                }
                else
                {
                    foreach (var pair in templateResource.GetResourceStreams(regexFilter))
                    {
                        var outputPath = Path.Combine(outputDirectory, pair.Key);
                        CopyResource(pair.Value, outputPath, overwrite);
                        Logger.Log(LogLevel.Verbose, $"File {pair.Key} copied to {outputPath}.");
                        isEmpty = false;
                    }
                }

                return !isEmpty;
            }
        }

        private static void CopyResource(Stream stream, string filePath, bool overwrite)
        {
            Copy(fs =>
            {
                stream.CopyTo(fs);
            }, filePath, overwrite);
        }

        private static void Copy(Action<Stream> streamHandler, string filePath, bool overwrite)
        {
            FileMode fileMode = overwrite ? FileMode.Create : FileMode.CreateNew;
            try
            {
                var subfolder = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(subfolder) && !Directory.Exists(subfolder))
                {
                    Directory.CreateDirectory(subfolder);
                }

                using (var fs = new FileStream(filePath, fileMode, FileAccess.ReadWrite, FileShare.ReadWrite))
                    streamHandler(fs);
            }
            catch (IOException e)
            {
                // If the file already exists, skip
                Logger.Log(LogLevel.Info, $"File {filePath}: {e.Message}, skipped");
            }
        }
    }
}
