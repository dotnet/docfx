using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Caching;
using Microsoft.Framework.Runtime.Compilation;
using Microsoft.Framework.Runtime.Roslyn;
using DnxProject = Microsoft.Framework.Runtime.Project;

namespace Microsoft.CodeAnalysis.Workspaces.Dnx
{
    public class ProjectModel
    {
        private static readonly CacheContextAccessor cacheContextAccessor = new CacheContextAccessor();
        private static readonly Cache cache = new Cache(cacheContextAccessor);
        private static readonly NamedCacheDependencyProvider namedCacheDependencyProvider = new NamedCacheDependencyProvider();

        public IDictionary<FrameworkName, ProjectInformation> Projects { get; } = new Dictionary<FrameworkName, ProjectInformation>();

        public static ProjectModel GetModel(string path)
        {
            var project = GetProject(path);

            if (project == null)
            {
                throw new InvalidOperationException("There's no project.json here");
            }

            var model = new ProjectModel();

            var sourcesProjectWideSources = project.Files.SourceFiles.ToList();

            foreach (var framework in project.GetTargetFrameworks())
            {
                var dependencySources = new List<string>(sourcesProjectWideSources);
                var dependencyInfo = ResolveDependencyInfo(project, framework.FrameworkName);

                // Add shared files from projects
                foreach (var reference in dependencyInfo.ProjectReferences)
                {
                    // Only add direct dependencies as sources
                    if (!project.Dependencies.Any(d => string.Equals(d.Name, reference.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    dependencySources.AddRange(reference.Project.Files.SharedFiles);
                }

                dependencySources.AddRange(dependencyInfo.ExportedSourcesFiles);

                var projectInfo = new ProjectInformation()
                {
                    Path = project.ProjectFilePath,
                    Project = project,
                    Configuration = "Debug",
                    Framework = framework.FrameworkName,
                    SourceFiles = dependencySources,
                    DependencyInfo = dependencyInfo,
                    CompilationSettings = project.GetCompilerOptions(framework.FrameworkName, "Debug")
                                                 .ToCompilationSettings(framework.FrameworkName)
                };

                model.Projects.Add(framework.FrameworkName, projectInfo);
            }

            return model;
        }

        private static DependencyInformation ResolveDependencyInfo(DnxProject project, FrameworkName frameworkName)
        {
            var cacheKey = Tuple.Create("DependencyInformation", project.Name, "Debug", frameworkName);

            return cache.Get<DependencyInformation>(cacheKey, ctx =>
            {
                var applicationHostContext = GetApplicationHostContext(project, "Debug", frameworkName);
                
                var info = new DependencyInformation
                {
                    HostContext = applicationHostContext,
                    ProjectReferences = new List<ProjectReference>(),
                    References = new List<string>(),
                    ExportedSourcesFiles = new List<string>()
                };

                foreach (var library in applicationHostContext.DependencyWalker.Libraries)
                {
                    // Skip unresolved libraries
                    if (!library.Resolved)
                    {
                        continue;
                    }

                    if (string.Equals(library.Type, "Project") &&
                       !string.Equals(library.Identity.Name, project.Name))
                    {
                        DnxProject referencedProject = GetProject(library.Path);

                        if (referencedProject == null)
                        {
                            // Should never happen
                            continue;
                        }

                        var targetFrameworkInformation = referencedProject.GetTargetFramework(library.Framework);

                        // If this is an assembly reference then treat it like a file reference
                        if (!string.IsNullOrEmpty(targetFrameworkInformation.AssemblyPath) &&
                            string.IsNullOrEmpty(targetFrameworkInformation.WrappedProject))
                        {
                            string assemblyPath = GetProjectRelativeFullPath(referencedProject, targetFrameworkInformation.AssemblyPath);
                            info.References.Add(assemblyPath);
                        }
                        else
                        {
                            string wrappedProjectPath = null;

                            if (!string.IsNullOrEmpty(targetFrameworkInformation.WrappedProject))
                            {
                                wrappedProjectPath = GetProjectRelativeFullPath(referencedProject, targetFrameworkInformation.WrappedProject);
                            }

                            info.ProjectReferences.Add(new ProjectReference
                            {
                                Name = referencedProject.Name,
                                Framework = library.Framework,
                                Path = library.Path,
                                WrappedProjectPath = wrappedProjectPath,
                                Project = referencedProject
                            });
                        }
                    }
                }

                var exportWithoutProjects = ProjectExportProviderHelper.GetExportsRecursive(
                     cache,
                     applicationHostContext.LibraryManager,
                     applicationHostContext.LibraryExportProvider,
                     new LibraryKey
                     {
                         Configuration = "Debug",
                         TargetFramework = frameworkName,
                         Name = project.Name
                     },
                     library => library.Type != "Project");

                foreach (var reference in exportWithoutProjects.MetadataReferences)
                {
                    var fileReference = reference as IMetadataFileReference;
                    if (fileReference != null)
                    {
                        info.References.Add(fileReference.Path);
                    }
                }

                foreach (var sourceFileReference in exportWithoutProjects.SourceReferences.OfType<ISourceFileReference>())
                {
                    info.ExportedSourcesFiles.Add(sourceFileReference.Path);
                }

                return info;
            });
        }

        private static ApplicationHostContext GetApplicationHostContext(DnxProject project, string configuration, FrameworkName frameworkName)
        {
            var cacheKey = Tuple.Create("ApplicationContext", project.Name, configuration, frameworkName);

            return cache.Get<ApplicationHostContext>(cacheKey, ctx =>
            {
                var applicationHostContext = new ApplicationHostContext(null,
                                                                        project.ProjectDirectory,
                                                                        packagesDirectory: null,
                                                                        configuration: configuration,
                                                                        targetFramework: frameworkName,
                                                                        cache: cache,
                                                                        cacheContextAccessor: cacheContextAccessor,
                                                                        namedCacheDependencyProvider: namedCacheDependencyProvider);

                applicationHostContext.DependencyWalker.Walk(project.Name, project.Version, frameworkName);

                return applicationHostContext;
            });
        }

        private static DnxProject GetProject(string path)
        {
            var cacheKey = Tuple.Create("Project", path);

            return cache.Get<DnxProject>(cacheKey, ctx =>
            {
                DnxProject project;
                DnxProject.TryGetProject(path, out project);
                return project;
            });
        }

        private static string GetProjectRelativeFullPath(DnxProject project, string path)
        {
            return Path.GetFullPath(Path.Combine(project.ProjectDirectory, path));
        }

        private class LibraryKey : ILibraryKey
        {
            public string Name { get; set; }

            public FrameworkName TargetFramework { get; set; }

            public string Configuration { get; set; }

            public string Aspect { get; set; }
        }
    }
}