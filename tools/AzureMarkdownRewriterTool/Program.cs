// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.AzureMarkdownRewriterTool
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.AzureMarkdownRewriters;
    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;

    internal sealed class Program
    {
        public static IReadOnlyList<string> SystemMarkdownFileName = new List<string> { "TOC.md" };

        public readonly bool _isMigration;
        public readonly string _srcDirectory;
        public readonly string _destDirectory;
        public readonly Dictionary<string, AzureFileInfo> _azureMarkdownFileInfoMapping;
        public readonly Dictionary<string, AzureFileInfo> _azureResourceFileInfoMapping;
        public readonly Dictionary<string, AzureVideoInfo> _azureVideoInfoMapping;

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

                var rewriterToolArguments = ParseRewriterToolArgumentsFile(args[0], args[1], args[2]);

                Dictionary<string, AzureVideoInfo> azureVideoInfoMapping = null;
                if (args.Length == 4)
                {
                    azureVideoInfoMapping = ParseAzureVideoFile(args[3]);
                    if (rewriterToolArguments == null)
                    {
                        return 1;
                    }
                }

                var azureFileInfo = GenerateAzureFileInfo(args[0], rewriterToolArguments, args[2]);

                foreach (var azureTransformArguments in rewriterToolArguments.AzureTransformArgumentsList)
                {
                    var p = new Program(rewriterToolArguments.IsMigration, azureTransformArguments.SourceDir, azureTransformArguments.DestDir, azureFileInfo.Item1, azureFileInfo.Item2, azureVideoInfoMapping);
                    if (!p.CheckParameters())
                    {
                        continue;
                    }

                    var result = p.Rewrite();
                    if (result != 0)
                    {
                        exitCode = result;
                    }

                    // Ignore this generate part currently
                    // p.GenerateTocForEveryFolder(new DirectoryInfo(p._destDirectory));
                }
                return exitCode;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
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

        private static Dictionary<string, AzureVideoInfo> ParseAzureVideoFile(string argsFilePath)
        {
            if (!File.Exists(argsFilePath))
            {
                Console.WriteLine("Can't find video mapping info file. Skip transform step for video.");
                return null;
            }

            var argsContent = File.ReadAllText(argsFilePath);
            try
            {
                var azureVideoInfoList = JsonConvert.DeserializeObject<List<AzureVideoInfo>>(argsContent);
                var azureVideoInfoMapping = new Dictionary<string, AzureVideoInfo>();
                foreach(var azureVideoInfo in azureVideoInfoList)
                {
                    azureVideoInfoMapping[azureVideoInfo.Id] = azureVideoInfo;
                }
                return azureVideoInfoMapping;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Azure vedio json deserialize failed. Skip transform step for video. args: {argsContent}. Ex: {e}");
                return null;
            }
        }

        private static Tuple<Dictionary<string, AzureFileInfo>, Dictionary<string, AzureFileInfo>> GenerateAzureFileInfo(string repositoryRoot, RewriterToolArguments rewriterToolArguments, string azureDocumentUriPrefix)
        {
            var azureMarkdownFileInfoMapping = new ConcurrentDictionary<string, AzureFileInfo>();
            var azureResourceFileInfoMapping = new ConcurrentDictionary<string, AzureFileInfo>();

            var files = Directory.GetFiles(repositoryRoot, "*", SearchOption.AllDirectories);
            Parallel.ForEach(
                files,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                file =>
                {
                    var relativePath = PathUtility.MakeRelativePath(repositoryRoot, file);
                    var isMarkdownFile = Path.GetExtension(relativePath).Equals(MarkdownExtension, StringComparison.OrdinalIgnoreCase);
                    if (IsIgnoreFile(relativePath, rewriterToolArguments.IsMigration))
                    {
                        return;
                    }

                    bool isSucceed = true;
                    var azureTransformArguments = rewriterToolArguments.AzureTransformArgumentsList.FirstOrDefault(a => PathUtility.IsPathUnderSpecificFolder(file, a.SourceDir));
                    var fileName = Path.GetFileName(file);

                    if (azureTransformArguments == null)
                    {
                        var azureFileInfo = new AzureFileInfo
                        {
                            FileName = fileName,
                            FilePath = PathUtility.NormalizePath(file),
                            NeedTransformToAzureExternalLink = true,
                            UriPrefix = azureDocumentUriPrefix
                        };

                        if (isMarkdownFile)
                        {
                            isSucceed = azureMarkdownFileInfoMapping.TryAdd(fileName, azureFileInfo);
                        }
                        else
                        {
                            isSucceed = azureResourceFileInfoMapping.TryAdd(fileName, azureFileInfo);
                        }
                    }
                    else
                    {
                        var azureFileInfo = new AzureFileInfo
                        {
                            FileName = fileName,
                            FilePath = PathUtility.NormalizePath(file),
                            NeedTransformToAzureExternalLink = false,
                            UriPrefix = azureTransformArguments.DocsHostUriPrefix,
                        };

                        if (isMarkdownFile)
                        {
                            isSucceed = azureMarkdownFileInfoMapping.TryAdd(fileName, azureFileInfo);
                        }
                        else
                        {
                            isSucceed = azureResourceFileInfoMapping.TryAdd(fileName, azureFileInfo);
                        }
                    }

                    if (!isSucceed)
                    {
                        Console.WriteLine($"GenerateAzureFileInfo failed: can't insert file with external prefix {file}");
                    }
                });

            return Tuple.Create(azureMarkdownFileInfoMapping.ToDictionary(m => m.Key, m => m.Value), azureResourceFileInfoMapping.ToDictionary(m => m.Key, m => m.Value));
        }

        private static bool IsIgnoreFile(string relativePath, bool isMigration)
        {
            if (relativePath.StartsWith(".") || relativePath.StartsWith("_site") || relativePath.StartsWith("log")
                || Path.GetFileName(relativePath).Equals("TOC.md", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Markdown file under includes file should be ignore. The resource file should also be calculated
            if (!isMigration)
            {
                if (relativePath.StartsWith("includes") && Path.GetExtension(relativePath).Equals(MarkdownExtension, StringComparison.OrdinalIgnoreCase))
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
            Dictionary<string, AzureFileInfo> azureMarkdownFileInfoMapping,
            Dictionary<string, AzureFileInfo> azureResourceFileInfoMapping,
            Dictionary<string, AzureVideoInfo> azureVideoInfoMapping)
        {
            _isMigration = isMigration;
            _srcDirectory = srcDirectory;
            _destDirectory = destDirectory;
            _azureMarkdownFileInfoMapping = azureMarkdownFileInfoMapping;
            _azureResourceFileInfoMapping = azureResourceFileInfoMapping;
            _azureVideoInfoMapping = azureVideoInfoMapping;
        }

        private bool CheckParameters()
        {
            if (!Directory.Exists(_srcDirectory))
            {
                Console.WriteLine($"Source directory not found: {_srcDirectory}. Stop transform.");
                return false;
            }

            if (_azureMarkdownFileInfoMapping == null)
            {
                Console.WriteLine($"Azure file info mapping is null. Stop transfrom");
                return false;
            }

            return true;
        }

        private int Rewrite()
        {
            var exitCode = 0;
            try
            {
                var consoleLogListener = new ConsoleLogListener();
                Logger.RegisterListener(consoleLogListener);

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
                                Console.WriteLine("Convert article {0}", fileInfo.FullName);
                                var source = File.ReadAllText(fileInfo.FullName);
                                string result;
                                if (_isMigration)
                                {
                                    result = AzureMigrationMarked.Markup(source, fileInfo.FullName, _azureMarkdownFileInfoMapping, _azureVideoInfoMapping, _azureResourceFileInfoMapping);
                                }
                                else
                                {
                                    result = AzureMarked.Markup(source, fileInfo.FullName, _azureMarkdownFileInfoMapping, _azureVideoInfoMapping, _azureResourceFileInfoMapping);
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
                            Console.WriteLine($"Transform article: {relativePathToSourceFolder} failed. Exception: {e}");
                        }
                    });
                Console.WriteLine("End transform dir '{0}' to dest dir '{1}' at {2}", _srcDirectory, _destDirectory, DateTime.UtcNow);
            }
            finally
            {
                Logger.Flush();
                Logger.UnregisterAllListeners();
            }
            return exitCode;
        }

        private void GenerateTocForEveryFolder(DirectoryInfo rootFolder)
        {
            foreach (var subFolder in rootFolder.GetDirectories())
            {
                GenerateTocForEveryFolder(subFolder);
            }

            var currentFolderTocPath = Path.Combine(rootFolder.FullName, "TOC.md");
            var currentFolderMdFiles = rootFolder.GetFiles("*.md", SearchOption.TopDirectoryOnly)
                                        .Where(fileInfo => !string.Equals(fileInfo.Name, "TOC.md", StringComparison.OrdinalIgnoreCase)).ToList();
            if (currentFolderMdFiles.Count == 0)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"# {rootFolder.Name}");
            foreach (var fileInfo in currentFolderMdFiles)
            {
                sb.AppendLine($"## [{fileInfo.Name}]({HttpUtility.UrlEncode(fileInfo.Name)})");
            }
            File.WriteAllText(currentFolderTocPath, sb.ToString());
        }
    }
}
