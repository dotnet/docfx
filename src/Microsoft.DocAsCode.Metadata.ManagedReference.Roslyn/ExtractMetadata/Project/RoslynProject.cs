// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    
    public class RoslynProject : AbstractProject
    {
        Project _project;

        public RoslynProject(Project project)
        {
            // Ensure that project references are unique, because
            // duplicate project references will fail Project.GetCompilationAsync.
            var groups = project.ProjectReferences.GroupBy(r => r);
            if (groups.Any(g => g.Count() > 1))
            {
                project = project.WithProjectReferences(groups.Select(g => g.Key));
            }

            _project = project;
        }

        public override string FilePath => _project.FilePath;

        public override bool HasDocuments => _project.HasDocuments;

        public override IEnumerable<AbstractDocument> Documents =>
            _project.Documents.Select(d => new RoslynDocument(d));

        public override IEnumerable<string> PortableExecutableMetadataReferences =>
            _project.MetadataReferences
            .Where(s => s is PortableExecutableReference)
            .Select(s => ((PortableExecutableReference)s).FilePath);

        public override IEnumerable<AbstractProject> ProjectReferences =>
            // TODO: cross-reference between Roslyn and F#
            _project.ProjectReferences.Select(pr =>
                new RoslynProject(_project.Solution.GetProject(pr.ProjectId)));

        public override async Task<AbstractCompilation> GetCompilationAsync()
        {
            var c = await _project.GetCompilationAsync();
            return new RoslynCompilation(c);
        }
    }
}
