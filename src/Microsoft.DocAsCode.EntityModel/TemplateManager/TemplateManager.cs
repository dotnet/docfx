// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.IO;
    using Utility;
    using System.Reflection;
    using System.Text;
    using System;

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

        public TemplateManager(Assembly assembly, string rootNamespace, string templateOverrideFolder, string templateName, string themeOverrideFolder, string themeName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                ParseResult.WriteToConsole(ResultLevel.Info, "Template is not specified.");
            }
            else
            {
                var templateResource = new ResourceFinder(assembly, rootNamespace, templateOverrideFolder).Find(templateName);

                if (templateResource != null)
                {
                    // js file does not exist, get the file with the same name as the template name
                    _templateProcessor = new TemplateProcessor(templateName, templateResource);
                }
                else
                {
                    ParseResult.WriteToConsole(ResultLevel.Warning, $"No template resource found for {templateName} from embedded resource {templateOverrideFolder}.");
                }
            }
            
            if (string.IsNullOrEmpty(themeName))
            {
                ParseResult.WriteToConsole(ResultLevel.Info, "Theme is not specified.");
            }
            else
            {
                _themeResource = new ResourceFinder(assembly, rootNamespace, themeOverrideFolder).Find(themeName);
                if (_themeResource == null)
                {
                    ParseResult.WriteToConsole(ResultLevel.Warning, $"No theme resource found for {themeName} from embedded resource {themeOverrideFolder}.");
                }
            }
        }

        public void ProcessTemplateAndTheme(IEnumerable<string> modelFilePaths, string baseDirectory, string outputDirectory, bool overwrite)
        {
            if (_templateProcessor != null)
            {
                ParseResult.WriteToConsole(ResultLevel.Info, "Template resource found, starting applying template.");
                _templateProcessor.Process(modelFilePaths, baseDirectory, outputDirectory);
            }

            if (_themeResource != null)
            {
                ParseResult.WriteToConsole(ResultLevel.Info, "Theme resource found, starting copying theme.");
                foreach (var resourceName in _themeResource.Names)
                {
                    using (var stream = _themeResource.GetResourceStream(resourceName))
                    {
                        var outputPath = Path.Combine(outputDirectory, resourceName);
                        CopyResource(stream, outputPath, overwrite);
                        ParseResult.WriteToConsole(ResultLevel.Info, $"Theme resource {resourceName} copied to {outputPath}.");
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
                            var relativePath = FileExtensions.MakeRelativePath(outputFolder, i);
                            writer.Write(string.Format(TocApi, relativePath));
                        }
                    if (conceptualFolder != null)
                        foreach (var i in conceptualFolder)
                        {
                            var relativePath = FileExtensions.MakeRelativePath(outputFolder, i);
                            writer.Write(string.Format(TocConceputal, relativePath));
                        }
                    ParseResult.WriteToConsole(ResultLevel.Info, message);
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
                ParseResult.WriteToConsole(ResultLevel.Info, "File {0}: {1}, skipped", filePath, e.Message);
            }
        }

        public void Dispose()
        {
            _themeResource?.Dispose();
            _templateProcessor?.Dispose();
        }
    }
    
}
