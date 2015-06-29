namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.DocAsCode.Utility;
    using System.Reflection;
    using System.Text;


    /// <summary>
    /// Template folder name matches template type
    /// </summary>
    [Flags]
    public enum TemplateType
    {
        Base = 0x0000,
        Github = 0x0010,
        IIS = 0x0100,
    }

    public class TemplateManager
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
        private static readonly TemplateType[] SupportedTemplateTypes = new TemplateType[] { TemplateType.Base, TemplateType.Github, TemplateType.IIS };

        /// <summary>
        /// Split manifest file with the predefined splitter, as manifest file naming has folder structure info lost
        /// </summary>
        private const char FolderSplitter = '~';
        
        public const string DefaultTocEntry = "toc.yml";


        /// <summary>
        /// TODO: follow grunt:copy
        /// Order matters as the latter Item could overwrite the former one
        /// </summary>
        public class ToCopyItem
        {
            public string[] Files { get; set; }
            public string CWD { get; set; }
            public string TargetFolder { get; set; }
        }

        public static void CopyItems(List<ToCopyItem> items, bool overwrite)
        {
            foreach(var item in items)
            {
                item.Files.CopyFilesToFolder(item.CWD, item.TargetFolder, overwrite, s => ParseResult.WriteToConsole(ResultLevel.Info, s), s => { ParseResult.WriteToConsole(ResultLevel.Warning, "Unable to copy file: {0}, ignored.", s); return true; });
            }
        }

        public static string GenerateDefaultToc(IEnumerable<string> apiFolder, IEnumerable<string> conceptualFolder, string outputFolder)
        {
            StringBuilder builder = new StringBuilder();
            if (apiFolder != null)
                foreach (var i in apiFolder)
                {
                    var relativePath = FileExtensions.MakeRelativePath(outputFolder, i);
                    builder.AppendFormat(TocApi, relativePath);
                }
            if (conceptualFolder != null)
                foreach (var i in conceptualFolder)
                {
                    var relativePath = FileExtensions.MakeRelativePath(outputFolder, i);
                    builder.AppendFormat(TocConceputal, relativePath);
                }

            return builder.ToString();
        }

        /// <summary>
        /// TODO: use GLOB
        /// </summary>
        public static void CopyToOutput(string workingDirectory, string rootNamespace, Assembly assembly, string customTemplateRootFolder, string outputFolderPath, string toc, TemplateType templateType = TemplateType.Base)
        {
            if (string.IsNullOrEmpty(workingDirectory)) workingDirectory = Environment.CurrentDirectory;
            if (string.IsNullOrEmpty(outputFolderPath)) outputFolderPath = workingDirectory;

            List<ToCopyItem> toCopyItems = new List<ToCopyItem>();

            IEnumerable<string> defaultTemplates = Enumerable.Empty<string>();

            // When no customized template specified:
            if (string.IsNullOrEmpty(customTemplateRootFolder))
            {
                CopyResources(assembly, rootNamespace, outputFolderPath, true, templateType);
            }
            else if (FilePathComparer.OSPlatformSensitiveComparer.Equals(customTemplateRootFolder, outputFolderPath))
            {
                ParseResult.WriteToConsole(ResultLevel.Warning, "Template folder {0} is the same as output folder {1}, templates will not be overwritten to the default one.", customTemplateRootFolder, outputFolderPath);
                CopyResources(assembly, rootNamespace, outputFolderPath, false, templateType);
            }
            else
            {
                toCopyItems.Add(new ToCopyItem { Files = Directory.GetFiles(customTemplateRootFolder), CWD = customTemplateRootFolder, TargetFolder = outputFolderPath });
                CopyItems(toCopyItems, true);
            }

            GenerateTocFile(toc, Path.Combine(outputFolderPath, DefaultTocEntry), false);
        }

        private static void CopyResources(Assembly assembly, string rootNamespace, string targetFolder, bool overwrite, TemplateType currentType)
        {
            List<string> baseNamespaces = new List<string>();
            var assemblyName = assembly.GetName().Name;
            string baseNamespaceFormatter = assemblyName;
            if (!string.IsNullOrEmpty(rootNamespace)) baseNamespaceFormatter += "." + rootNamespace;
            baseNamespaceFormatter += ".{0}.";
            foreach (var type in SupportedTemplateTypes)
            {
                if (currentType.HasFlag(type))
                {
                    baseNamespaces.Add(string.Format(baseNamespaceFormatter, type));
                }
            }

            foreach (var resource in assembly.GetManifestResourceNames())
            {
                CopyResource(assembly, resource, baseNamespaces, targetFolder, overwrite);
            }
        }

        private static void CopyResource(Assembly assembly, string resource, IEnumerable<string> baseNamespaces, string targetFolder, bool overwrite)
        {
            string baseNamespace = baseNamespaces.FirstOrDefault(s => resource.StartsWith(s));

            if (baseNamespace == null) return;

            var rawFileName = resource.Substring(baseNamespace.Length);

            // Get subfolder if the file name is splitted by '~'
            var fileName = rawFileName.Replace(FolderSplitter, '/');
            var filePath = Path.Combine(targetFolder, fileName);
            bool fileExists = File.Exists(filePath);

            if (!overwrite && fileExists)
            {
                ParseResult.WriteToConsole(ResultLevel.Info, "File {0} already exists, skipped", filePath);
                return;
            }

            var subfolder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(subfolder) && !Directory.Exists(subfolder))
            {
                Directory.CreateDirectory(subfolder);
            }

            using (var stream = assembly.GetManifestResourceStream(resource))
            {
                if (overwrite)
                {
                    using (var streamWriter = new FileStream(filePath, FileMode.Create))
                    {
                        stream.CopyTo(streamWriter);
                    }
                }
                else
                {
                    try
                    {
                        using (var streamWriter = new FileStream(filePath, FileMode.CreateNew))
                        {
                            stream.CopyTo(streamWriter);
                        }

                    }
                    catch (IOException e)
                    {
                        ParseResult.WriteToConsole(ResultLevel.Info, "File {0}: {1}, skipped", filePath, e.Message);
                    }
                }
            }
        }

        private static void AddToCopyItems(List<ToCopyItem> item, string rootFolder, string outputFolder, TemplateType currentType, params TemplateType[] types)
        {
            foreach (var type in types)
            {
                if (currentType.HasFlag(type))
                {
                    string baseFolder = Path.Combine(rootFolder, type.ToString());
                    try
                    {
                        item.Add(new ToCopyItem { Files = Directory.GetFiles(baseFolder, "*", SearchOption.AllDirectories), CWD = baseFolder, TargetFolder = outputFolder });
                    }
                    catch (Exception e)
                    {
                        ParseResult.WriteToConsole(ResultLevel.Warning, "Unable to get files from {0}, ignored: {1}", baseFolder, e.Message);
                    }
                }
            }
        }

        private static void GenerateTocFile(string toc, string targetTocPath, bool overwrite)
        {
            if (string.IsNullOrEmpty(toc)) return;
            if (overwrite)
            {
                File.WriteAllText(targetTocPath, toc);
                ParseResult.WriteToConsole(ResultLevel.Info, "Root toc.yml {0} is overwritten.", targetTocPath);
                return;
            }

            if (!File.Exists(targetTocPath))
            {
                try
                {
                    using (var stream = new FileStream(targetTocPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(toc);
                    }
                    ParseResult.WriteToConsole(ResultLevel.Info, "Root toc.yml {0} is not found, default toc.yml is generated.", targetTocPath);
                }
                catch (IOException)
                {
                    // If the file already exists, skip
                }
            }
        }
        private static void CopyTemplate(string templateFolder, string destination, string excludedFolder, StringComparison stringComparison)
        {
            IList<string> files = Directory.GetFiles(templateFolder, "*", SearchOption.AllDirectories);
            if (!string.IsNullOrEmpty(excludedFolder))
            {
                var normalizedTemplateFolder = templateFolder.ToNormalizedFullPath();
                var normalizedExcluedFolder = excludedFolder.ToNormalizedFullPath();

                // If template folder is the folder to be excluded, do nothing
                if (normalizedTemplateFolder.Equals(normalizedExcluedFolder, stringComparison)) return;

                // If template folder contains excludedFolder, should exclude every file inside excludedFolder
                if (normalizedExcluedFolder.StartsWith(normalizedTemplateFolder + "/", stringComparison))
                {
                    if (Directory.Exists(excludedFolder))
                    {
                        try
                        {
                            files = files.Except(Directory.GetFiles(excludedFolder, "*", SearchOption.AllDirectories), FilePathComparer.OSPlatformSensitiveComparer).Select(s => s.ToNormalizedFullPath()).ToList();
                        }
                        catch (DirectoryNotFoundException)
                        {
                            // Ignore the excluded folder if it does not exists
                        }
                    }
                    ParseResult.WriteToConsole(ResultLevel.Warning, "Copying template from {0} to {1}, excluding {2}", normalizedTemplateFolder, destination, normalizedExcluedFolder);
                }
            }

            files.CopyFilesToFolder(templateFolder, destination, true, s => ParseResult.WriteToConsole(ResultLevel.Info, s), s => { ParseResult.WriteToConsole(ResultLevel.Warning, "Unable to copy file: {0}, ignored.", s); return true; });
        }
    }
}
