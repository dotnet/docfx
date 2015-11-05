// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Builders;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Utility;

    public class TemplateManager : IDisposable
    {
        private const string TemplateEntry = "index.html";

        private const string TocApi = @"
- name: Api {0}
  href: {0}
";

        private const string TocConceputal = @"
- name: {0}
  href: {0}
";

        public const string DefaultTocEntry = "toc.yml";

        private TemplateProcessor _templateProcessor;

        private ResourceCollection _themeResource = null;

        public TemplateManager(Assembly assembly, string rootNamespace, List<string> templates, List<string> themes)
        {
            var resourceFinder = new ResourceFinder(assembly, rootNamespace);
            if (templates == null || templates.Count == 0)
            {
                Logger.Log(LogLevel.Info, "Template is not specified, files will not be transformed.");
            }
            else
            {
                var templateResources = templates.Select(s => resourceFinder.Find(s)).Where(s => s != null).ToArray();
                if (templateResources.Length == 0)
                {
                    Logger.Log(LogLevel.Warning, $"No template resource found for [{templates.ToDelimitedString()}].");
                }
                else
                {
                    _templateProcessor = new TemplateProcessor(new CompositeResourceCollectionWithOverridden(templateResources));
                }
            }
            
            if (themes == null || themes.Count == 0)
            {
                Logger.Log(LogLevel.Info, "Theme is not specified, no additional theme will be applied to the documentation.");
            }
            else
            {
                var themeResources = themes.Select(s => resourceFinder.Find(s)).Where(s => s != null).ToArray();
                if (themeResources.Length == 0)
                {
                    Logger.Log(LogLevel.Warning, $"No theme resource found for [{themes.ToDelimitedString()}].");
                }
                else
                {
                    _themeResource = new CompositeResourceCollectionWithOverridden(themeResources);
                }
            }
        }

        public void ProcessTemplateAndTheme(DocumentBuildContext context, string outputDirectory, bool overwrite)
        {
            if (_templateProcessor != null)
            {
                Logger.Log(LogLevel.Verbose, "Template resource found, starting applying template.");
                _templateProcessor.Process(context, outputDirectory);
            }

            if (_themeResource != null)
            {
                Logger.Log(LogLevel.Verbose, "Theme resource found, starting copying theme.");
                foreach (var resourceName in _themeResource.Names)
                {
                    using (var stream = _themeResource.GetResourceStream(resourceName))
                    {
                        var outputPath = Path.Combine(outputDirectory, resourceName);
                        CopyResource(stream, outputPath, overwrite);
                        Logger.Log(LogLevel.Info, $"Theme resource {resourceName} copied to {outputPath}.");
                    }
                }
            }
        }

        public static void GenerateDefaultToc(IEnumerable<string> apiFolder, IEnumerable<string> conceptualFolder, string outputFolder, bool overwrite)
        {
            if (string.IsNullOrEmpty(outputFolder)) outputFolder = Environment.CurrentDirectory;
            var targetTocPath = Path.Combine(outputFolder, DefaultTocEntry);
            var message = overwrite ? $"Root toc.yml {targetTocPath} is overwritten." : $"Root toc.yml {targetTocPath} is not found, default toc.yml is generated.";
            Copy(s =>
            {
                using (var writer = new StreamWriter(s))
                {
                    if (apiFolder != null)
                        foreach (var i in apiFolder)
                        {
                            var relativePath = PathUtility.MakeRelativePath(outputFolder, i);
                            writer.Write(string.Format(TocApi, relativePath));
                        }
                    if (conceptualFolder != null)
                        foreach (var i in conceptualFolder)
                        {
                            var relativePath = PathUtility.MakeRelativePath(outputFolder, i);
                            writer.Write(string.Format(TocConceputal, relativePath));
                        }
                    Logger.Log(LogLevel.Info, message);
                }
            }, targetTocPath, overwrite);
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

        public void Dispose()
        {
            _themeResource?.Dispose();
            _templateProcessor?.Dispose();
        }
    }
    
}
