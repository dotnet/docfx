// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.AzureMarkdownRewriterTool
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.DocAsCode.AzureMarkdownRewriters;
    using Microsoft.DocAsCode.Common;

    using Newtonsoft.Json;

    internal sealed class Program
    {
        private static readonly Regex _azureHtmlIncludeWithPrefixRegex = new Regex(@"^(\<br\s*\/\>)(\s*\r?\n\[AZURE\.INCLUDE)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex _azureHtmlIncludeWithPostfixRegex = new Regex(@"^(\[AZURE\.INCLUDE\s*\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([^)]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)\])[\t\f ]*(\S.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex _azureHtmlDefinitionWithLeadingWhitespacesRegex = new Regex(@"^( +)(\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\r?\n+|$))", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static IReadOnlyList<string> SystemMarkdownFileName = new List<string> { "TOC.md" };

        public readonly bool _isMigration;
        public readonly string _srcDirectory;
        public readonly string _destDirectory;
        public readonly AzureFileInformationCollection _azureFileInformationCollection;

        private const string MarkdownExtension = ".md";

        private static int Main(string[] args)
        {
            try
            {
                var exitCode = 0;
                if (args.Length != 3 && args.Length != 4)
                {
                    PrintUsage();
                    return 1;
                }

                // Parse the basic arguments
                var rewriterToolArguments = ParseRewriterToolArgumentsFile(args[0], args[1], args[2]);
                if (rewriterToolArguments == null)
                {
                    return 1;
                }

                // Register logger
                var consoleLogListener = new ConsoleLogListener();
                var htmlLogFile = Path.Combine(rewriterToolArguments.AzureTransformArgumentsList.First().SourceDir, "log", "log.html");
                if (File.Exists(htmlLogFile))
                {
                    File.Delete(htmlLogFile);
                }
                var htmlLogListener = new HtmlLogListener(htmlLogFile);
                Logger.RegisterListener(consoleLogListener);
                Logger.RegisterListener(htmlLogListener);

                // Parse advanced migration parameters
                AzureFileInformationCollection azureFileInformationCollection = new AzureFileInformationCollection();
                if (args.Length == 4)
                {
                    azureFileInformationCollection.AzureVideoInfoMapping = AzureVideoHelper.ParseAzureVideoFile(args[3], rewriterToolArguments.IsMigration);
                }

                if (rewriterToolArguments.IsMigration)
                {
                    GenerateAzureFileInfoForMigration(args[0], rewriterToolArguments, args[2], azureFileInformationCollection);
                }
                else
                {
                    GenerateAzureFileInfo(args[0], rewriterToolArguments, args[2], azureFileInformationCollection);
                }

                foreach (var azureTransformArguments in rewriterToolArguments.AzureTransformArgumentsList)
                {
                    var p = new Program(rewriterToolArguments.IsMigration, azureTransformArguments.SourceDir, azureTransformArguments.DestDir, azureFileInformationCollection);
                    if (!p.CheckParameters())
                    {
                        continue;
                    }

                    var result = p.Rewrite();
                    if (result != 0)
                    {
                        exitCode = result;
                    }
                }
                return exitCode;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
            finally
            {
                Logger.Flush();
                Logger.UnregisterAllListeners();
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\t{0} <repositoryRoot> <AzureTransformArgumentsFilePath> <azureDocumentUriPrefix> [<azureVideoMappingFilePath>]", AppDomain.CurrentDomain.FriendlyName);
        }

        private static RewriterToolArguments ParseRewriterToolArgumentsFile(string repositoryRoot, string argsFilePath, string azureDocumentUriPrefix)
        {
            if (!File.Exists(argsFilePath) || !Directory.Exists(repositoryRoot))
            {
                Console.WriteLine("Can't find args file path or repo folder, won't do transform step.");
                return null;
            }

            if (string.IsNullOrEmpty(azureDocumentUriPrefix))
            {
                Console.WriteLine("Azure external uri pre fix is null or empty. Won't do transform step");
                return null;
            }

            var argsContent = File.ReadAllText(argsFilePath);
            try
            {
                return JsonConvert.DeserializeObject<RewriterToolArguments>(argsContent);
            }
            catch (Exception)
            {
                try
                {
                    var azureTransformArgumentsList = JsonConvert.DeserializeObject<List<AzureTransformArguments>>(argsContent);
                    return new RewriterToolArguments(azureTransformArgumentsList, false);
                }
                catch (Exception e2)
                {
                    Console.WriteLine($"Azure args json deserialize failed. Won't do transform step. args: {argsContent}. Ex: {e2}");
                    return null;
                }
            }
        }

        private static bool GenerateAzureFileInfo(
            string repositoryRoot,
            RewriterToolArguments rewriterToolArguments,
            string azureDocumentUriPrefix,
            AzureFileInformationCollection azureFileInformationCollection)
        {
            var azureMarkdownFileInfoMapping = new ConcurrentDictionary<string, AzureFileInfo>();
            var azureResourceFileInfoMapping = new ConcurrentDictionary<string, AzureFileInfo>();

            var files = Directory.GetFiles(repositoryRoot, "*", SearchOption.AllDirectories);
            Parallel.ForEach(
                files,
                new ParallelOptions { MaxDegreeOfParallelism = 8 },
                file =>
                {
                    var relativePath = PathUtility.MakeRelativePath(repositoryRoot, file);
                    if (IsIgnoreFile(relativePath, rewriterToolArguments.IsMigration))
                    {
                        return;
                    }

                    var isSucceed = true;
                    var azureTransformArguments = rewriterToolArguments.AzureTransformArgumentsList.FirstOrDefault(a => PathUtility.IsPathUnderSpecificFolder(file, a.SourceDir));

                    // By default, all the link should be transformed to external link with azure uri prefix
                    // However, if we find that the file is under one of the folder that need to be transformed. Then the prefix uri should be docs but not auzre
                    var needTransformToAzureExternalLink = true;
                    var uriPrefix = azureDocumentUriPrefix;
                    if (azureTransformArguments != null)
                    {
                        needTransformToAzureExternalLink = false;
                        uriPrefix = azureTransformArguments.DocsHostUriPrefix;
                    }

                    var fileName = Path.GetFileName(file);
                    var azureFileInfo = new AzureFileInfo
                    {
                        FileName = fileName,
                        FilePath = PathUtility.NormalizePath(file),
                        NeedTransformToAzureExternalLink = needTransformToAzureExternalLink,
                        UriPrefix = uriPrefix
                    };

                    AzureFileInfo conflictFile;
                    var isMarkdownFile = Path.GetExtension(relativePath).Equals(MarkdownExtension, StringComparison.OrdinalIgnoreCase);
                    if (isMarkdownFile)
                    {
                        isSucceed = azureMarkdownFileInfoMapping.TryAdd(fileName, azureFileInfo);
                        azureMarkdownFileInfoMapping.TryGetValue(fileName, out conflictFile);
                    }
                    else
                    {
                        isSucceed = azureResourceFileInfoMapping.TryAdd(fileName, azureFileInfo);
                        azureResourceFileInfoMapping.TryGetValue(fileName, out conflictFile);
                    }

                    if (!isSucceed)
                    {
                        Console.WriteLine($"GenerateAzureFileInfo warning: can't insert file: {file}, confilicts with: {conflictFile?.FilePath}");
                    }
                });

            azureFileInformationCollection.AzureMarkdownFileInfoMapping = azureMarkdownFileInfoMapping.ToDictionary(m => m.Key, m => m.Value);
            azureFileInformationCollection.AzureResourceFileInfoMapping = azureResourceFileInfoMapping.ToDictionary(m => m.Key, m => m.Value);
            return true;
        }

        private static bool GenerateAzureFileInfoForMigration(
            string repositoryRoot,
            RewriterToolArguments rewriterToolArguments,
            string azureDocumentUriPrefix,
            AzureFileInformationCollection azureFileInformationCollection)
        {
            var azureMarkdownFileInfoMapping = new ConcurrentDictionary<string, AzureFileInfo>();
            var azureResourceFileInfoMapping = new ConcurrentDictionary<string, AzureFileInfo>();
            var azureIncludeMarkdownFileInfoMapping = new ConcurrentDictionary<string, AzureFileInfo>();
            var azureIncludeResourceFileInfoMapping = new ConcurrentDictionary<string, AzureFileInfo>();

            bool hasDupliateMdFileName = false;
            var files = Directory.GetFiles(repositoryRoot, "*", SearchOption.AllDirectories);
            Parallel.ForEach(
                files,
                new ParallelOptions { MaxDegreeOfParallelism = 8 },
                file =>
                {
                    var relativePath = PathUtility.MakeRelativePath(repositoryRoot, file);
                    if (IsIgnoreFile(relativePath, rewriterToolArguments.IsMigration))
                    {
                        return;
                    }

                    var filePath = PathUtility.NormalizePath(file);
                    var fileName = Path.GetFileName(file);
                    var azureFileInfo = new AzureFileInfo
                    {
                        FileName = fileName,
                        FilePath = PathUtility.NormalizePath(file),
                        NeedTransformToAzureExternalLink = false,
                        UriPrefix = string.Empty
                    };

                    var isIncludeFile = filePath.Split(new[] { Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
                                            .Any(folder => folder.Equals("includes", StringComparison.OrdinalIgnoreCase));
                    var isMarkdownFile = Path.GetExtension(relativePath).Equals(MarkdownExtension, StringComparison.OrdinalIgnoreCase);

                    AzureFileInfo conflictFile = null;
                    var isSucceed = true;
                    if (!isIncludeFile && isMarkdownFile)
                    {
                        isSucceed = azureMarkdownFileInfoMapping.TryAdd(fileName, azureFileInfo);
                        azureMarkdownFileInfoMapping.TryGetValue(fileName, out conflictFile);
                    }
                    else if (!isIncludeFile && !isMarkdownFile)
                    {
                        // For resource file, even if has conflicts, we regards that as succeed
                        azureResourceFileInfoMapping.TryAdd(fileName, azureFileInfo);
                    }
                    else if (isIncludeFile && isMarkdownFile)
                    {
                        isSucceed = azureIncludeMarkdownFileInfoMapping.TryAdd(fileName, azureFileInfo);
                        azureIncludeMarkdownFileInfoMapping.TryGetValue(fileName, out conflictFile);
                    }
                    else
                    {
                        // For resource file, even if has conflicts, we regards that as succeed
                        azureIncludeResourceFileInfoMapping.TryAdd(fileName, azureFileInfo);
                    }

                    if (!isSucceed)
                    {
                        hasDupliateMdFileName = true;
                        Logger.LogError($"Error: GenerateAzureFileInfo failed. File: {file} name confilicts with: {conflictFile?.FilePath}");
                    }
                });

            azureFileInformationCollection.AzureMarkdownFileInfoMapping = azureMarkdownFileInfoMapping.ToDictionary(m => m.Key, m => m.Value);
            azureFileInformationCollection.AzureResourceFileInfoMapping = azureResourceFileInfoMapping.ToDictionary(m => m.Key, m => m.Value);
            azureFileInformationCollection.AzureIncludeMarkdownFileInfoMapping = azureIncludeMarkdownFileInfoMapping.ToDictionary(m => m.Key, m => m.Value);
            azureFileInformationCollection.AzureIncludeResourceFileInfoMapping = azureIncludeResourceFileInfoMapping.ToDictionary(m => m.Key, m => m.Value);

            return !hasDupliateMdFileName;
        }

        private static bool IsIgnoreFile(string relativePath, bool isMigration)
        {
            if (relativePath.StartsWith(".") || relativePath.StartsWith("_site") || relativePath.StartsWith("log")
                || Path.GetFileName(relativePath).Equals("TOC.md", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!isMigration)
            {
                // For non-migration case, markdown file under includes file should be ignore. The resource file should also be calculated
                if (relativePath.StartsWith("includes") && Path.GetExtension(relativePath).Equals(MarkdownExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else
            {
                // For migration case, files under templates should be ignored, otherwise there'll be some tokens/properties can't be resolved.
                if (relativePath.StartsWith("markdown templates"))
                {
                    return true;
                }
            }

            return false;
        }

        public Program(
            bool isMigration,
            string srcDirectory,
            string destDirectory,
            AzureFileInformationCollection azureFileInformationCollection)
        {
            _isMigration = isMigration;
            _srcDirectory = srcDirectory;
            _destDirectory = destDirectory;
            _azureFileInformationCollection = azureFileInformationCollection;
        }

        private bool CheckParameters()
        {
            if (!Directory.Exists(_srcDirectory))
            {
                Console.WriteLine($"Source directory not found: {_srcDirectory}. Stop transform.");
                return false;
            }

            if (_azureFileInformationCollection == null)
            {
                Console.WriteLine($"Azure file info mapping is null. Stop transfrom");
                return false;
            }

            return true;
        }

        // Removes "<br/>" or "<br />" before "[AZURE.INCLUDE".
        private string FixAzureIncludeWithPrefixSyntax(string source)
        {
            return _azureHtmlIncludeWithPrefixRegex.Replace(source, "$2");
        }

        // Moves the content after "[AZURE.INCLUDE" section to the next line.
        // For example, "[AZURE.INCLUDE [active-directory-devquickstarts-switcher](../../includes/active-directory-devquickstarts-switcher.md)] test"
        // should be converted to 
        // "[AZURE.INCLUDE [active-directory-devquickstarts-switcher](../../includes/active-directory-devquickstarts-switcher.md)]\r\ntest"
        private string FixAzureIncludeWithPostfixSyntax(string source)
        {
            return _azureHtmlIncludeWithPostfixRegex.Replace(source, $"$1{Environment.NewLine}$6");
        }

        // Removes all leading white-spaces for definition.
        private string FixAzureDefinitionWithLeadingWhitespacesSyntax(string source)
        {
            return _azureHtmlDefinitionWithLeadingWhitespacesRegex.Replace(source, "$2");
        }

        private int Rewrite()
        {
            var exitCode = 0;
            var sourceDirInfo = new DirectoryInfo(_srcDirectory);
            var fileInfos = sourceDirInfo.GetFiles("*.md", SearchOption.AllDirectories);

            Console.WriteLine("Start transform dir '{0}' to dest dir '{1}' at {2}", _srcDirectory, _destDirectory, DateTime.UtcNow);
            Parallel.ForEach(
                fileInfos,
                new ParallelOptions() { MaxDegreeOfParallelism = 8 },
                fileInfo =>
                {
                    var relativePathToSourceFolder = fileInfo.FullName.Substring(_srcDirectory.Length + 1);
                    try
                    {
                        if (IsIgnoreFile(relativePathToSourceFolder, _isMigration))
                        {
                            return;
                        }
                        var outputPath = Path.Combine(_destDirectory, relativePathToSourceFolder);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        if (string.Equals(fileInfo.Extension, MarkdownExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            var source = File.ReadAllText(fileInfo.FullName);
                            string result;
                            if (_isMigration)
                            {
                                // Fixs Azure articles first in order to let docfx parse them correctly.
                                source = FixAzureIncludeWithPrefixSyntax(source);
                                source = FixAzureIncludeWithPostfixSyntax(source);
                                source = FixAzureDefinitionWithLeadingWhitespacesSyntax(source);

                                result = AzureMigrationMarked.Markup(
                                    source,
                                    fileInfo.FullName,
                                    _azureFileInformationCollection.AzureMarkdownFileInfoMapping,
                                    _azureFileInformationCollection.AzureResourceFileInfoMapping,
                                    _azureFileInformationCollection.AzureIncludeMarkdownFileInfoMapping,
                                    _azureFileInformationCollection.AzureIncludeResourceFileInfoMapping,
                                    _azureFileInformationCollection.AzureVideoInfoMapping);
                            }
                            else
                            {
                                result = AzureMarked.Markup(
                                    source,
                                    fileInfo.FullName,
                                    _azureFileInformationCollection.AzureMarkdownFileInfoMapping,
                                    _azureFileInformationCollection.AzureVideoInfoMapping,
                                    _azureFileInformationCollection.AzureResourceFileInfoMapping);
                            }
                            File.WriteAllText(outputPath, result);
                        }
                        else
                        {
                            //Console.WriteLine("Copy file {0} to output path {1}", fileInfo.FullName, outputPath);
                            //File.Copy(fileInfo.FullName, outputPath, true);
                        }
                    }
                    catch (Exception e)
                    {
                        exitCode = 1;
                        Console.Write($"System Error: Processing File: { relativePathToSourceFolder }. Error: Migration failed. Exception: {e}.");
                    }
                });
            Console.WriteLine("End transform dir '{0}' to dest dir '{1}' at {2}", _srcDirectory, _destDirectory, DateTime.UtcNow);
            return exitCode;
        }
    }
}
