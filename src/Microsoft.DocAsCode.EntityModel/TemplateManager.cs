namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Utility;
    using System.Reflection;
    using System.Text;
    using System.IO.Compression;
    
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

        public const string DefaultTocEntry = "toc.yml";

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
        public static void CopyToOutput(string workingDirectory, string rootNamespace, Assembly assembly, string customTemplateRootFolder, string outputFolderPath, string toc, string themeName)
        {
            if (string.IsNullOrEmpty(themeName)) themeName = "default";
            if (string.IsNullOrEmpty(workingDirectory)) workingDirectory = Environment.CurrentDirectory;
            if (string.IsNullOrEmpty(outputFolderPath)) outputFolderPath = workingDirectory;

            IEnumerable<string> defaultTemplates = Enumerable.Empty<string>();

            if (FilePathComparer.OSPlatformSensitiveComparer.Equals(customTemplateRootFolder, outputFolderPath))
            {
                ParseResult.WriteToConsole(ResultLevel.Warning, "Template folder {0} is the same as output folder {1}, templates will not be overwritten to the default one.", customTemplateRootFolder, outputFolderPath);
                CopyResources(assembly, rootNamespace, outputFolderPath, null, false, themeName);
            }
            else
            {
                CopyResources(assembly, rootNamespace, outputFolderPath, customTemplateRootFolder, true, themeName);
            }

            GenerateTocFile(toc, Path.Combine(outputFolderPath, DefaultTocEntry), false);
        }

        private static void CopyResources(Assembly assembly, string rootNamespace, string targetFolder, string customTemplateRootFolder, bool overwrite, string themeName)
        {
            var assemblyName = assembly.GetName().Name;
            var prefix = string.Format("{0}.{1}.", assemblyName, rootNamespace);
            var resourceNames = GetThemeNames(themeName);
            var embeddedThemes = assembly.GetManifestResourceNames();
            IEnumerable<string> availableBuiltinThemes = embeddedThemes.Select(s => Path.GetFileNameWithoutExtension(s).Substring(prefix.Length));
            IEnumerable<string> matchedBuiltinThemes = availableBuiltinThemes.Intersect(resourceNames, StringComparer.OrdinalIgnoreCase);

            var embedded = embeddedThemes.Where(s=> matchedBuiltinThemes.Contains(Path.GetFileNameWithoutExtension(s).Substring(prefix.Length)));
            var notFoundThemes = resourceNames.Except(matchedBuiltinThemes, StringComparer.OrdinalIgnoreCase);
            if (notFoundThemes.Any())
            {
                ParseResult.WriteToConsole(ResultLevel.Info, "Did not find any matching builtin themes for '{0}'.", notFoundThemes.ToDelimitedString());
            }
            else
            {
                foreach (var resource in embedded)
                {
                    ParseResult.WriteToConsole(ResultLevel.Info, "Builtin '{0}' theme package found, start unzipping template into target folder '{1}'.", resource, targetFolder);
                    using (var stream = assembly.GetManifestResourceStream(resource))
                    {
                        UnzipTemplate(stream, targetFolder, overwrite);
                    }
                }
            }

            IEnumerable<string> availableCustomThemes = null;
            IEnumerable<string> matchedCustomThemes = null;

            // Search custom template folder, templates inside custom template folder overrides the embeded resources
            if (!string.IsNullOrEmpty(customTemplateRootFolder) && Directory.Exists(customTemplateRootFolder))
            {
                var customThemes = Directory.GetFiles(customTemplateRootFolder, "*.zip", SearchOption.AllDirectories);
                availableCustomThemes = customThemes.Select(s => Path.GetFileNameWithoutExtension(s));
                matchedCustomThemes = availableCustomThemes.Intersect(resourceNames, StringComparer.OrdinalIgnoreCase);

                var customs = customThemes.Where(s => matchedCustomThemes.Contains(Path.GetFileNameWithoutExtension(s)));
                notFoundThemes = notFoundThemes.Except(matchedCustomThemes, StringComparer.OrdinalIgnoreCase);
                if (notFoundThemes.Any())
                {
                    ParseResult.WriteToConsole(ResultLevel.Info, "Did not find any matching custom themes for '{0}'.", notFoundThemes.ToDelimitedString());
                }
                else
                {
                    foreach (var resource in customs)
                    {
                        ParseResult.WriteToConsole(ResultLevel.Info, "'{0}' theme package found, start unzipping template into target folder '{1}'.", resource, targetFolder);
                        using (var stream = File.OpenRead(resource))
                        {
                            UnzipTemplate(stream, targetFolder, overwrite);
                        }
                    }
                }
            }

            if (notFoundThemes.Any())
            {
                var message = string.Format("Unable to find any matching themes for '{0}'. The available builtin themes are '{1}' while the matching ones are '{2}'. ", notFoundThemes.ToDelimitedString(), availableBuiltinThemes.ToDelimitedString(), matchedBuiltinThemes.ToDelimitedString());
                if (!string.IsNullOrEmpty(customTemplateRootFolder))
                {
                    if (!Directory.Exists(customTemplateRootFolder)) message += string.Format("The custom template folder '{0}' does not exist", customTemplateRootFolder);
                    else message += string.Format("The themes available in the custom template folder '{0}' are '{1}' while the matching ones are '{2}'", customTemplateRootFolder, availableCustomThemes.ToDelimitedString(), matchedCustomThemes.ToDelimitedString());
                }
                ParseResult.WriteToConsole(ResultLevel.Error, message);
            }
        }
        
        /// <summary>
        /// Theme names are combined by `.`: [theme 1].[theme 2].[theme 3] combines theme 1, 2, 3 inorder and the latter one will override the former one if name collapses
        /// Order matters
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<string> GetThemeNames(string themeName)
        {
            return themeName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void UnzipTemplate(Stream zippedStream, string targetFolder, bool overwrite)
        {
            List<string> rootSubFolder = new List<string>();
            using (ZipArchive zip = new ZipArchive(zippedStream))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    // When Name is empty, entry is folder, ignore
                    if (!string.IsNullOrEmpty(entry.Name))
                    {
                        string targetPath = Path.Combine(targetFolder, entry.FullName);
                        using(var entryStream = entry.Open())
                        {
                            CopyResource(entryStream, targetPath, overwrite);
                        }
                    }
                }
            }
        }

        private static void CopyResource(Stream stream, string filePath, bool overwrite)
        {
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
    }
}
