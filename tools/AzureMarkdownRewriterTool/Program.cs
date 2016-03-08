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

    using Microsoft.DocAsCode.AzureMarkdownRewriters;
    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;

    internal sealed class Program
    {
        public static IReadOnlyList<string> SystemMarkdownFileName = new List<string> { "TOC.md" };

        public readonly string _srcDirectory;
        public readonly string _destDirectory;
        public readonly Dictionary<string, AzureFileInfo> _azureFileInfoMapping;
        public readonly Dictionary<string, AzureVideoInfo> _azureVideoInfoMapping
            = new Dictionary<string, AzureVideoInfo>
            {
                ["azure-ad--introduction-to-dynamic-memberships-for-groups"] =
                    new AzureVideoInfo
                    {
                        Id = "azure-ad--introduction-to-dynamic-memberships-for-groups",
                        Link = "https://channel9.msdn.com/Series/Azure-Active-Directory-Videos-Demos/Azure-AD--Introduction-to-Dynamic-Memberships-for-Groups/player/",
                        Height = 360,
                        Width = 640
                    },
                ["enable-single-sign-on-to-google-apps-in-2-minutes-with-azure-ad"] =
                    new AzureVideoInfo
                    {
                        Id = "enable-single-sign-on-to-google-apps-in-2-minutes-with-azure-ad",
                        Link = "https://channel9.msdn.com/Series/Azure-Active-Directory-Videos-Demos/Enable-single-sign-on-to-Google-Apps-in-2-minutes-with-Azure-AD/player/",
                        Height = 360,
                        Width = 640
                    },
                ["integrating-salesforce-with-azure-ad-how-to-enable-single-sign-on"] =
                    new AzureVideoInfo
                    {
                        Id = "integrating-salesforce-with-azure-ad-how-to-enable-single-sign-on",
                        Link = "http://channel9.msdn.com/Series/Azure-Active-Directory-Videos-Demos/Integrating-Salesforce-with-Azure-AD-How-to-enable-Single-Sign-On-12/player/",
                        Height = 360,
                        Width = 640
                    },
                ["integrating-salesforce-with-azure-ad-how-to-automate-user-provisioning"] =
                    new AzureVideoInfo
                    {
                        Id = "integrating-salesforce-with-azure-ad-how-to-automate-user-provisioning",
                        Link = "http://channel9.msdn.com/Series/Azure-Active-Directory-Videos-Demos/Integrating-Salesforce-with-Azure-AD-How-to-automate-User-Provisioning-22/player/",
                        Height = 360,
                        Width = 640
                    }
            };

        private const string MarkdownExtension = ".md";

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length != 3)
                {
                    PrintUsage();
                }

                var azureTransformArgumentsList = ParseAzureTransformArgumentsFile(args[0], args[1], args[2]);
                if (azureTransformArgumentsList == null)
                {
                    return 0;
                }

                var azureFileInfoMapping = GenerateAzureFileInfo(args[0], azureTransformArgumentsList, args[2]);

                foreach (var azureTransformArguments in azureTransformArgumentsList)
                {
                    var p = new Program(azureTransformArguments.SourceDir, azureTransformArguments.DestDir, azureFileInfoMapping);
                    if (!p.CheckParameters())
                    {
                        continue;
                    }
                    p.Rewrite();

                    // Ignore this generate part currently
                    // p.GenerateTocForEveryFolder(new DirectoryInfo(p._destDirectory));
                }
                return 0;
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
            Console.WriteLine("\t{0} <repositoryRoot> <AzureTransformArgumentsFilePath> <azureDocumentUriPrefix>", AppDomain.CurrentDomain.FriendlyName);
        }

        private static List<AzureTransformArguments> ParseAzureTransformArgumentsFile(string repositoryRoot, string argsFilePath, string azureDocumentUriPrefix)
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
                return JsonConvert.DeserializeObject<List<AzureTransformArguments>>(argsContent);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Json deserialize failed. Won't do transform step. args: {argsContent}. Ex: {e}");
                return null;
            }
        }

        private static Dictionary<string, AzureFileInfo> GenerateAzureFileInfo(string repositoryRoot, List<AzureTransformArguments> azureTransformArgumentsList, string azureDocumentUriPrefix)
        {
            var azureFileInfoMapping = new ConcurrentDictionary<string, AzureFileInfo>();
            var repositoryRootPathLength = repositoryRoot.Length;
            var files = Directory.EnumerateFiles(repositoryRoot, "*.md", SearchOption.AllDirectories);
            Parallel.ForEach(
                files,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                file =>
                {
                    var relativePath = PathUtility.MakeRelativePath(repositoryRoot, file);
                    if (IsIgnoreFile(relativePath))
                    {
                        return;
                    }

                    var azureTransformArguments = azureTransformArgumentsList.FirstOrDefault(a => PathUtility.IsPathUnderSpecificFolder(file, a.SourceDir));
                    var fileName = Path.GetFileName(file);
                    bool isSucceed = true;
                    if (azureTransformArguments == null)
                    {
                        isSucceed = azureFileInfoMapping.TryAdd(
                            fileName,
                            new AzureFileInfo
                            {
                                FileName = fileName,
                                FilePath = PathUtility.NormalizePath(file),
                                NeedTransformToAzureExternalLink = true,
                                UriPrefix = azureDocumentUriPrefix
                            });
                    }
                    else
                    {
                        isSucceed = azureFileInfoMapping.TryAdd(
                            fileName,
                            new AzureFileInfo
                            {
                                FileName = fileName,
                                FilePath = PathUtility.NormalizePath(file),
                                NeedTransformToAzureExternalLink = false,
                                UriPrefix = azureTransformArguments.DocsHostUriPrefix,
                            });
                    }

                    if (!isSucceed)
                    {
                        Console.WriteLine($"GenerateAzureFileInfo failed: can't insert file with external prefix {file}");
                    }
                });

            return azureFileInfoMapping.ToDictionary(m => m.Key, m => m.Value);
        }

        private static bool IsIgnoreFile(string relativePath)
        {
            if (relativePath.StartsWith(".") || relativePath.StartsWith("_site") || relativePath.StartsWith("log")
                || relativePath.StartsWith("includes") || Path.GetFileName(relativePath).Equals("TOC.md", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public Program(string srcDirectory, string destDirectory, Dictionary<string, AzureFileInfo> azureFileInfoMapping)
        {
            _srcDirectory = srcDirectory;
            _destDirectory = destDirectory;
            _azureFileInfoMapping = azureFileInfoMapping;
        }

        private bool CheckParameters()
        {
            if (!Directory.Exists(_srcDirectory))
            {
                Console.WriteLine($"Source directory not found: {_srcDirectory}. Stop transform.");
                return false;
            }

            if (_azureFileInfoMapping == null)
            {
                Console.WriteLine($"Azure file info mapping is null. Stop transfrom");
            }

            return true;
        }

        private void Rewrite()
        {
            var sourceDirInfo = new DirectoryInfo(_srcDirectory);
            var fileInfos = sourceDirInfo.EnumerateFiles("*.md", SearchOption.AllDirectories);

            Console.WriteLine("Start transform dir '{0}' to dest dir '{1}' at {2}", _srcDirectory, _destDirectory, DateTime.UtcNow);
            Parallel.ForEach(
                fileInfos,
                new ParallelOptions() { MaxDegreeOfParallelism = 8 },
                fileInfo =>
                {
                    var relativePathToSourceFolder = fileInfo.FullName.Substring(_srcDirectory.Length + 1);
                    if (IsIgnoreFile(relativePathToSourceFolder))
                    {
                        return;
                    }
                    var outputPath = Path.Combine(_destDirectory, relativePathToSourceFolder);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    if (string.Equals(fileInfo.Extension, MarkdownExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Convert article {0}", fileInfo.FullName);
                        var source = File.ReadAllText(fileInfo.FullName);
                        var result = AzureMarked.Markup(source, fileInfo.FullName, _azureFileInfoMapping, _azureVideoInfoMapping);
                        File.WriteAllText(outputPath, result);
                    }
                    else
                    {
                        //Console.WriteLine("Copy file {0} to output path {1}", fileInfo.FullName, outputPath);
                        //File.Copy(fileInfo.FullName, outputPath, true);
                    }
                });
            Console.WriteLine("End transform dir '{0}' to dest dir '{1}' at {2}", _srcDirectory, _destDirectory, DateTime.UtcNow);
        }

        private void GenerateTocForEveryFolder(DirectoryInfo rootFolder)
        {
            foreach (var subFolder in rootFolder.EnumerateDirectories())
            {
                GenerateTocForEveryFolder(subFolder);
            }

            var currentFolderTocPath = Path.Combine(rootFolder.FullName, "TOC.md");
            var currentFolderMdFiles = rootFolder.EnumerateFiles("*.md", SearchOption.TopDirectoryOnly)
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
