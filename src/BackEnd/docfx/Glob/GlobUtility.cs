using Microsoft.DocAsCode.EntityModel;
using Microsoft.DocAsCode.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace docfx
{
    internal class GlobUtility
    {
        /// <summary>
        /// TODO: Only exclude files contained in include files to improve performance
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetFilesFromFileMappingItem(string baseDirectory, FileMappingItem item)
        {
            if (item == null)
            {
                return Enumerable.Empty<string>();
            }
            var includeFiles = GetFilesFromGlobPatterns(baseDirectory, item.Files);
            var excludeFiles = GetFilesFromGlobPatterns(baseDirectory, item.Exclude);
            return includeFiles.Except(excludeFiles);
        }

        public static FileMapping ExpandFileMapping(string baseDirectory, FileMapping fileMapping, Func<string, string> keyGenerator)
        {
            if (fileMapping == null)
            {
                return null;
            }
            var expandedFileMapping = new FileMapping();

            foreach (var item in fileMapping.Items)
            {
                // Use local variable to avoid different items influencing each other
                string workingDirectory = baseDirectory;
                if (!string.IsNullOrEmpty(item.CurrentWorkingDirectory))
                {
                    // If key is empty, set it to the default api subfolder
                    if (string.IsNullOrEmpty(workingDirectory))
                    {
                        workingDirectory = item.CurrentWorkingDirectory;
                    }
                    else
                    {
                        workingDirectory = Path.Combine(workingDirectory, item.CurrentWorkingDirectory);
                    }
                }

                string key = keyGenerator(item.Name);
                var files = GetFilesFromFileMappingItem(workingDirectory, item).ToList();
                if (files.Count == 0)
                {
                    ParseResult.WriteToConsole(ResultLevel.Info, "No files are found with glob pattern {0}, excluding {1}, under working directory {2}", item.Files.ToDelimitedString() ?? "<none>", item.Exclude.ToDelimitedString() ?? "<none>", baseDirectory ?? "<current>");
                }
                expandedFileMapping.Add(
                    new FileMappingItem
                    {
                        CurrentWorkingDirectory = workingDirectory,
                        Files = new FileItems(files),
                        Name = key,
                    });
            }

            return expandedFileMapping;
        }

        public static IEnumerable<string> GetFilesFromGlobPatterns(string baseDirectory, IEnumerable<string> projects, Func<string, IEnumerable<string>> filesProvider = null)
        {
            if (projects == null)
            {
                return Enumerable.Empty<string>();
            }
            return (from s in GetProjects(baseDirectory, projects, filesProvider)
                    select s.ToNormalizedFullPath()).Distinct(FilePathComparer.OSPlatformSensitiveComparer);
        }

        /// <summary>
        /// Normalize path as Glob take '\' as escape character
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetProjects(string baseDirectory, IEnumerable<string> projects, Func<string, IEnumerable<string>> filesProvider = null)
        {
            if (projects == null)
            {
                return Enumerable.Empty<string>();
            }
            return from project in
                       (from s in projects
                        select s.ToNormalizedPath()).Distinct()
                   from validFile in GlobPathHelper.GetFiles(baseDirectory, project, filesProvider)
                   select validFile;
        }
    }
}
