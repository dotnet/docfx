// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.Utility;

    public class TemplateManager
    {
        private readonly List<string> _templates = new List<string>();
        private readonly List<string> _themes = new List<string>();
        private readonly ResourceFinder _finder;
        public TemplateManager(Assembly assembly, string rootNamespace, List<string> templates, List<string> themes, string baseDirectory)
        {
            _finder = new ResourceFinder(assembly, rootNamespace, baseDirectory);
            if (templates == null || templates.Count == 0)
            {
                Logger.Log(LogLevel.Info, "Template is not specified, files will not be transformed.");
            }
            else
            {
                _templates = templates;
            }
            
            if (themes == null || themes.Count == 0)
            {
                Logger.Log(LogLevel.Info, "Theme is not specified, no additional theme will be applied to the documentation.");
            }
            else
            {
                _themes = themes;
            }
        }

        /// <summary>
        /// Template can contain a set of plugins to define the behavior of how to generate the output YAML data model
        /// The name of plugin folder is always "plugins"
        /// </summary>
        public IEnumerable<KeyValuePair<string, Stream>> GetTemplatePlugins()
        {
            using (var templateResource = new CompositeResourceCollectionWithOverridden(_templates.Select(s => _finder.Find(s)).Where(s => s != null)))
            {
                if (templateResource.IsEmpty)
                {
                    yield break;
                }
                else
                {
                    foreach (var pair in templateResource.GetResourceStreams(@"^plugins/.*"))
                    {
                        yield return pair;
                    }
                }
            }
        }

        public void ProcessTemplateAndTheme(DocumentBuildContext context, string outputDirectory, bool overwrite)
        {
            ProcessTemplate(context, outputDirectory);
            ProcessTheme(outputDirectory, overwrite);
        }

        public bool TryExportTemplateFiles(string outputDirectory, string regexFilter = null)
        {
            return TryExportResourceFiles(_templates, outputDirectory, true, regexFilter);
        }

        private void ProcessTemplate(DocumentBuildContext context, string outputDirectory)
        {
            using (var templateResource = new CompositeResourceCollectionWithOverridden(_templates.Select(s => _finder.Find(s)).Where(s => s != null)))
            {
                if (templateResource.IsEmpty)
                {
                    Logger.Log(LogLevel.Warning, $"No template resource found for [{_templates.ToDelimitedString()}].");
                }
                else
                {
                    Logger.Log(LogLevel.Verbose, "Template resource found, starting applying template.");
                    using (var processor = new TemplateProcessor(templateResource))
                    {
                        processor.Process(context, outputDirectory);
                    }
                }
            }
        }

        private void ProcessTheme(string outputDirectory, bool overwrite)
        {
            if (_themes == null || _themes.Count == 0) return;
            TryExportResourceFiles(_themes, outputDirectory, overwrite);
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
