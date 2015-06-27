namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.Utility;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.MSBuild;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;

    internal class IncrementalCheck
    {
        private static readonly Lazy<MSBuildWorkspace> Workspace = new Lazy<MSBuildWorkspace>(() => MSBuildWorkspace.Create());

        private VersionStamp _versionToBeCompared;

        private ConcurrentDictionary<string, VersionStamp> _metadataVersionCache;
        private AsyncConcurrentCache<string, bool> _projectUpToDateSnapshot;
        private bool _versionChanged;
        public IncrementalCheck(BuildInfo buildInfo)
        {
            var checkUtcTime = buildInfo.TriggeredUtcTime;
            var version = buildInfo.BuildAssembly;
            var currentVersion = CacheBase.AssemblyName;
            if (currentVersion != version)
            {
                _versionChanged = true;
                if (_versionChanged)
                {
                    ParseResult.WriteToConsole(ResultLevel.Verbose, "Assembly '{0}' when last build took place is not current assembly '{1}', rebuild required", version ?? "<undefined>", currentVersion);
                }
            }

            _versionToBeCompared = VersionStamp.Create(checkUtcTime);
            _metadataVersionCache = new ConcurrentDictionary<string, VersionStamp>();
            _projectUpToDateSnapshot = new AsyncConcurrentCache<string, bool>();
        }

        public bool AreFilesModified(IEnumerable<string> files)
        {
            if (_versionChanged) return true;
            foreach (var file in files)
            {
                if (IsFileModified(file)) return true;
            }

            return false;
        }

        /// <summary>
        /// If file does not exists, return **true**?? ==> should have checked exists before calling.
        /// If file's last modified time is newer, return true; otherwise, return false
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool IsFileModified(string file)
        {
            if (_versionChanged) return true;
            if (string.IsNullOrEmpty(file)) return false;
            if (!File.Exists(file))
            {
                ParseResult.WriteToConsole(ResultLevel.Verbose, "File '{0}' does not exist anymore, rebuild needed", file);
                return true;
            }

            var version = GetLastModifiedVersionForFile(file);
            if (VersionNewer(version))
            {
                ParseResult.WriteToConsole(ResultLevel.Verbose, "File '{0}' version '{1}' newer than '{2}'", file, version.ToString(), _versionToBeCompared.ToString());
                return true;
            }
            else
            {
                ParseResult.WriteToConsole(ResultLevel.Verbose, "File '{0}' version '{1}' older than '{2}', no need to rebuild", file, version.ToString(), _versionToBeCompared.ToString());
            }

            return false;
        }

        private bool VersionNewer(VersionStamp thisVersion)
        {
            return VersionNewer(thisVersion, _versionToBeCompared);
        }

        private static bool VersionNewer(VersionStamp thisVersion, VersionStamp thatVersion)
        {
            var version = thisVersion.GetNewerVersion(thatVersion);
            if (version == thisVersion) return true;
            return false;
        }

        /// <summary>
        /// Load all the version this project source code dependent on: 
        /// 1. project file version; 
        /// 2. document version; 
        /// 3. assembly reference version
        /// TODO: In which case do project references not in current solution?
        /// And save to global storage
        /// </summary>
        /// <param name="project"></param>
        public async Task<bool> IsSingleProjectChanged(Project project)
        {
            if (_versionChanged) return true;
            // 1. project file itself changed since <date>
            var version = GetLastModifiedVersionForFile(project.FilePath);

            if (VersionNewer(version))
            {
                ParseResult.WriteToConsole(ResultLevel.Verbose, "project file '{0}' version '{1}' newer than '{2}'", project.Name, version.ToString(), _versionToBeCompared.ToString());
                return true;
            }
            else
            {
                ParseResult.WriteToConsole(ResultLevel.Verbose, "project file '{0}' version '{1}' older than '{2}', no need to rebuild", project.Name, version.ToString(), _versionToBeCompared.ToString());
            }

            // 2. project's containing source files changed since <date>
            var documents = project.Documents;
            foreach (var document in documents)
            {
                // Incase new document added into project however project file is not changed
                // e.g. in kproj or csproj by <Compile Include="*.cs"/> 
                VersionStamp documentVersion = await document.GetTextVersionAsync();
                var path = document.FilePath;
                if (!string.IsNullOrEmpty(path))
                {
                    var createdTime = GetCreatedVersionForFile(path);
                    documentVersion = VersionNewer(documentVersion, createdTime) ? documentVersion : createdTime;
                }

                if (VersionNewer(documentVersion))
                {
                    ParseResult.WriteToConsole(ResultLevel.Verbose, "document '{0}' version '{1}' newer than '{2}'", document.Name, documentVersion.ToString(), _versionToBeCompared.ToString());
                    return true;
                }
                else
                {
                    ParseResult.WriteToConsole(ResultLevel.Verbose, "document '{0}' version '{1}' older than '{2}', no need to rebuild", document.Name, documentVersion.ToString(), _versionToBeCompared.ToString());
                }
            }

            // 3. project's assembly reference changed since <date>
            var assemblyReferences = project.MetadataReferences;
            foreach (var assemblyReference in assemblyReferences)
            {
                var executableReference = assemblyReference as PortableExecutableReference;
                if (executableReference != null)
                {
                    var filePath = executableReference.FilePath;
                    var assemblyVersion = _metadataVersionCache.GetOrAdd(filePath, s => GetLastModifiedVersionForFile(s));
                    if (VersionNewer(assemblyVersion))
                    {
                        Console.WriteLine("document {0} version {1} newer than {2}", filePath, assemblyVersion, _versionToBeCompared);
                        return true;
                    }
                }
            }

            // TODO: In which case do project references not in current solution?
            // EXAMPLE: <Roslyn>/VBCSCompilerTests, contains 11 project references, however 9 of them are in solution
            // vbc2.vcxproj and vbc2.vcxproj are not in solution. Currently consider it as irrelavate to source code rebuild
            var projectReferences = project.AllProjectReferences;

            return false;
        }

        private static VersionStamp GetLastModifiedVersionForFile(string filePath)
        {
            var dateTime = File.GetLastWriteTimeUtc(filePath);
            return VersionStamp.Create(dateTime);
        }

        private static VersionStamp GetCreatedVersionForFile(string filePath)
        {
            var dateTime = File.GetCreationTimeUtc(filePath);
            return VersionStamp.Create(dateTime);
        }
    }
}
