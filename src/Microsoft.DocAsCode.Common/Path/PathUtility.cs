// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// 
    /// </summary>
    public static class PathUtility
    {
        private static readonly Regex UriWithProtocol = new Regex(@"^\w{2,}\:", RegexOptions.Compiled);
        private static readonly char[] InvalidFilePathChars = Path.GetInvalidFileNameChars();
        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

        public static string ToValidFilePath(this string input, char replacement = '_')
        {
            if (string.IsNullOrEmpty(input))
            {
                return Path.GetRandomFileName();
            }

            string validPath = new string(input.Select(s => InvalidFilePathChars.Contains(s) ? replacement : s).ToArray());

            return validPath;
        }

        public static void SaveFileListToFile(List<string> fileList, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || fileList == null || fileList.Count == 0)
            {
                return;
            }

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                fileList.ForEach(s => writer.WriteLine(s));
            }
        }

        public const string ListFileExtension = ".list";

        public static List<string> GetFileListFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            List<string> fileList = new List<string>();

            if (Path.GetExtension(filePath) == ListFileExtension)
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    while (!reader.EndOfStream)
                    {
                        fileList.Add(reader.ReadLine());
                    }
                }
            }

            return fileList;
        }

        /// <summary>
        /// http://stackoverflow.com/questions/422090/in-c-sharp-check-that-filename-is-possibly-valid-not-that-it-exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsVaildFilePath(string path)
        {
            FileInfo fi = null;
            try
            {
                fi = new FileInfo(path);
            }
            catch (ArgumentException) { }
            catch (System.IO.PathTooLongException) { }
            catch (NotSupportedException) { }
            return fi != null;
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="basePath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="absolutePath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string MakeRelativePath(string basePath, string absolutePath)
        {
            if (string.IsNullOrEmpty(basePath)) return absolutePath;
            if (string.IsNullOrEmpty(absolutePath)) return null;
            if (FilePathComparer.OSPlatformSensitiveComparer.Equals(basePath, absolutePath)) return string.Empty;

            // Append / to the directory
            if (basePath[basePath.Length - 1] != '/')
            {
                basePath = basePath + "/";
            }

            Uri fromUri = new Uri(Path.GetFullPath(basePath));
            Uri toUri = new Uri(Path.GetFullPath(absolutePath));

            if (fromUri.Scheme != toUri.Scheme) { return absolutePath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.ToUpperInvariant() == "FILE")
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath.BackSlashToForwardSlash();
        }

        public static IEnumerable<string> CopyFilesToFolder(this IEnumerable<string> files, string sourceFolder, string destinationFolder, bool overwrite, Action<string> messageHandler, Func<string, bool> errorHandler)
        {
            IList<string> targetFiles = new List<string>();
            if (files == null) return null;
            if (FilePathComparer.OSPlatformSensitiveComparer.Equals(sourceFolder, destinationFolder))
            {
                return null;
            }

            foreach (var file in files)
            {
                try
                {
                    var relativePath = MakeRelativePath(sourceFolder, file);
                    var destinationPath = string.IsNullOrEmpty(destinationFolder) ? relativePath : Path.Combine(destinationFolder, relativePath);

                    if (!FilePathComparer.OSPlatformSensitiveComparer.Equals(file, destinationPath))
                    {
                        if (messageHandler != null) messageHandler(string.Format("Copying file from '{0}' to '{1}'", file, destinationPath));
                        var targetDirectory = Path.GetDirectoryName(destinationPath);
                        if (!string.IsNullOrEmpty(targetDirectory)) Directory.CreateDirectory(targetDirectory);

                        File.Copy(file, destinationPath, overwrite);
                        targetFiles.Add(destinationPath);
                    }
                    else
                    {
                        if (messageHandler != null) messageHandler(string.Format("{0} is already latest.", destinationPath));
                    }
                }
                catch (Exception e)
                {
                    if (!overwrite)
                    {
                        if (e is IOException) continue;
                    }
                    if (errorHandler != null)
                    {
                        if (errorHandler(e.Message))
                        {
                            continue;
                        }
                        else
                        {
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (targetFiles.Count == 0) return null;
            return targetFiles;
        }

        public static string GetFullPath(string folder, string href)
        {
            if (string.IsNullOrEmpty(href)) return null;
            if (string.IsNullOrEmpty(folder)) return href;
            return Path.GetFullPath(Path.Combine(folder, href));
        }

        public static void CopyFile(string path, string targetPath, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(targetPath)) return;
            if (FilePathComparer.OSPlatformSensitiveComparer.Equals(path, targetPath)) return;
            var targetFolder = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetFolder)) Directory.CreateDirectory(targetFolder);

            File.Copy(path, targetPath, overwrite);
        }

        public static bool IsRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            // IsWellFormedUriString does not try to escape characters such as '\' ' ', '(', ')' and etc. first. Use TryCreate instead
            Uri absoluteUri;
            if (Uri.TryCreate(path, UriKind.Absolute, out absoluteUri))
            {
                return false;
            }

            if (UriWithProtocol.IsMatch(path))
            {
                return false;
            }

            foreach (var ch in InvalidPathChars)
            {
                if (path.Contains(ch))
                {
                    return false;
                }
            }

            return !Path.IsPathRooted(path);
        }

        /// <summary>
        /// Also change backslash to forward slash
        /// </summary>
        /// <param name="path"></param>
        /// <param name="kind"></param>
        /// <param name="basePath"></param>
        /// <returns></returns>
        public static string FormatPath(this string path, UriKind kind, string basePath = null)
        {
            if (kind == UriKind.RelativeOrAbsolute)
            {
                return path.BackSlashToForwardSlash();
            }
            if (kind == UriKind.Absolute)
            {
                return Path.GetFullPath(path).BackSlashToForwardSlash();
            }
            if (kind == UriKind.Relative)
            {
                if (string.IsNullOrEmpty(basePath))
                {
                    return path.BackSlashToForwardSlash();
                }

                return MakeRelativePath(basePath, path).BackSlashToForwardSlash();
            }

            return null;
        }

        public static bool IsPathUnderSpecificFolder(string path, string folder)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(folder))
            {
                return false;
            }
            return NormalizePath(path).StartsWith(NormalizePath(folder) + Path.AltDirectorySeparatorChar);
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }
            return Path.GetFullPath(path).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static bool IsDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException($"{nameof(path)} should not be null or empty string");
            }
            return File.GetAttributes(path).HasFlag(FileAttributes.Directory);
        }
    }
}