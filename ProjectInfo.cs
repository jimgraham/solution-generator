using System.Collections.Generic;
using Microsoft.Build.BuildEngine;

namespace SolGen
{
    /// <summary>
    /// Represents a project reference or loaded project.
    /// </summary>
    internal class ProjectInfo
    {

        public string ProjectName = null;
        public string ProjectGuid = null;
        public string Filename = null;
        public string AssemblyName = null;
        public string FolderGuid = null;

        // list of assemblynames
        public List<string> References = new List<string>();

        public Project MsBuildProject;

        public ProjectInfo ShallowCopy()
        {
            return (ProjectInfo) this.MemberwiseClone();
        }
    }
}
