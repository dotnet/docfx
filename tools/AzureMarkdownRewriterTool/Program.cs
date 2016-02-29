namespace Microsoft.DocAsCode.Tools.AzureMarkdownRewriterTool
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.DocAsCode.AzureMarkdownRewriters;

    internal sealed class Program
    {
        public readonly string _srcDirectory;
        public readonly string _destDirectory;

        private const string MarkdownExtension = ".md";

        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                PrintUsage();
            }

            try
            {
                var p = new Program(args[0], args[1]);
                p.CheckParameters();
                p.Rewrite();

                // Ignore this generate part currently
                // p.GenerateTocForEveryFolder(new DirectoryInfo(p._destDirectory));
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
            Console.WriteLine("\t{0} <srcDirectory> <destDirectory>", AppDomain.CurrentDomain.FriendlyName);
        }

        public Program(string srcDirectory, string destDirectory)
        {
            _srcDirectory = srcDirectory;
            _destDirectory = destDirectory;
        }

        private void CheckParameters()
        {
            if (!Directory.Exists(_srcDirectory))
            {
                throw new ArgumentException($"Source directory not found: {_srcDirectory}");
            }
        }

        private void Rewrite()
        {
            var sourceDirInfo = new DirectoryInfo(_srcDirectory);
            var fileInfos = sourceDirInfo.EnumerateFiles("*", SearchOption.AllDirectories);

            Console.WriteLine("Start at {0}", DateTime.UtcNow);
            Parallel.ForEach(
                fileInfos,
                new ParallelOptions() { MaxDegreeOfParallelism = 8 },
                fileInfo =>
                {
                    var relativePathToSourceFolder = fileInfo.FullName.Substring(_srcDirectory.Length + 1);
                    if (relativePathToSourceFolder.StartsWith(".") || relativePathToSourceFolder.StartsWith("_site") || relativePathToSourceFolder.StartsWith("log"))
                    {
                        return;
                    }
                    var outputPath = Path.Combine(_destDirectory, relativePathToSourceFolder);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    if (string.Equals(fileInfo.Extension, MarkdownExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Convert article {0}", fileInfo.FullName);
                        var source = File.ReadAllText(fileInfo.FullName);
                        var result = AzureMarked.Markup(source, fileInfo.FullName);
                        File.WriteAllText(outputPath, result);
                    }
                    else
                    {
                        //Console.WriteLine("Copy file {0} to output path {1}", fileInfo.FullName, outputPath);
                        //File.Copy(fileInfo.FullName, outputPath, true);
                    }
                });
            Console.WriteLine("End at {0}", DateTime.UtcNow);
        }

        private void GenerateTocForEveryFolder(DirectoryInfo rootFolder)
        {
            foreach (var subFolder in rootFolder.EnumerateDirectories())
            {
                GenerateTocForEveryFolder(subFolder);
            }

            var currentFolderTocPath = Path.Combine(rootFolder.FullName, "TOC.md");
            var currentFolderMdFiles = rootFolder.EnumerateFiles("*.md", SearchOption.TopDirectoryOnly)
                                        .Where(fileInfo => !string.Equals(fileInfo.Name, "TOC.md", StringComparison.OrdinalIgnoreCase));
            if (currentFolderMdFiles.Count() == 0)
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
