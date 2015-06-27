using System;
using System.Collections.Generic;
using Microsoft.Framework.Runtime;

namespace Microsoft.CodeAnalysis.Workspaces.Dnx
{
    // Represents the dependencies of a particular project
    public class DependencyInformation
    {
        public ApplicationHostContext HostContext { get; set; }

        // List of references to files on disk
        public IList<string> References { get; set; }

        // List of project references
        public IList<ProjectReference> ProjectReferences { get; set; }

        // Source files exported from this project
        public IList<string> ExportedSourcesFiles { get; set; }
    }
}