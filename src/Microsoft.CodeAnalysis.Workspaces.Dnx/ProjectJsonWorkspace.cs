using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Workspaces.Dnx
{
    public class ProjectJsonWorkspace : Workspace
    {
        private Dictionary<string, AssemblyMetadata> _cache = new Dictionary<string, AssemblyMetadata>();

        private readonly string[] _projectPaths;

        public ProjectJsonWorkspace(string projectPath) : this(new[] { projectPath })
        {
        }

        public ProjectJsonWorkspace(string[] projectPaths) : base(MefHostServices.DefaultHost, "Custom")
        {
            _projectPaths = projectPaths;

            Initialize();
        }

        private void Initialize()
        {
            foreach (var projectPath in _projectPaths)
            {
                AddProject(projectPath);
            }
        }

        private void AddProject(string projectPath)
        {
            var model = ProjectModel.GetModel(projectPath);

            // Get all of the specific projects (there is a project per framework)
            foreach (var p in model.Projects.Values)
            {
                AddProject(p);
            }
        }

        private ProjectId AddProject(ProjectInformation project)
        {
            // Create the framework specific project and add it to the workspace
            var projectInfo = ProjectInfo.Create(
                                ProjectId.CreateNewId(),
                                VersionStamp.Create(),
                                project.Project.Name + "+" + project.Framework,
                                project.Project.Name,
                                LanguageNames.CSharp,
                                project.Path);

            OnProjectAdded(projectInfo);

            OnParseOptionsChanged(projectInfo.Id, new CSharpParseOptions(project.CompilationSettings.LanguageVersion, preprocessorSymbols: project.CompilationSettings.Defines));

            OnCompilationOptionsChanged(projectInfo.Id, project.CompilationSettings.CompilationOptions);

            foreach (var file in project.SourceFiles)
            {
                using (var stream = File.OpenRead(file))
                {
                    var sourceText = SourceText.From(stream, encoding: Encoding.UTF8);
                    var id = DocumentId.CreateNewId(projectInfo.Id);
                    var version = VersionStamp.Create();
                    
                    var loader = TextLoader.From(TextAndVersion.Create(sourceText, version));
                    OnDocumentAdded(DocumentInfo.Create(id, file, filePath: file, loader: loader));
                }
            }

            foreach (var path in project.DependencyInfo.References)
            {
                OnMetadataReferenceAdded(projectInfo.Id, GetMetadataReference(path));
            }

            foreach (var reference in project.DependencyInfo.ProjectReferences)
            {
                var pe = ProjectModel.GetModel(reference.Path);

                // This being null would me a broken project reference
                var projectReference = pe.Projects[reference.Framework];

                var id = AddProject(projectReference);
                OnProjectReferenceAdded(projectInfo.Id, new Microsoft.CodeAnalysis.ProjectReference(id));
            }

            return projectInfo.Id;
        }

        private MetadataReference GetMetadataReference(string path)
        {
            AssemblyMetadata assemblyMetadata;
            if (!_cache.TryGetValue(path, out assemblyMetadata))
            {
                using (var stream = File.OpenRead(path))
                {
                    var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                    assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                    _cache[path] = assemblyMetadata;
                }
            }
            
            return assemblyMetadata.GetReference();
        }
    }
}