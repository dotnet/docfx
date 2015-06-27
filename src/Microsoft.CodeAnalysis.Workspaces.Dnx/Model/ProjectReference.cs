using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;
using DnxProject = Microsoft.Framework.Runtime.Project;

namespace Microsoft.CodeAnalysis.Workspaces.Dnx
{
    // Represents a project reference
    public class ProjectReference
    {
        public FrameworkName Framework { get; set; }
        
        public string Name { get; set; }
        
        public string Path { get; set; }

        public string WrappedProjectPath { get; set; }

        public DnxProject Project { get; set; }
    }
}