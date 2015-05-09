using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolGen
{
    /// <summary>
    /// Represents command-line arguments.
    /// </summary>
    internal class Arguments
    {
        public List<string> ProbeFolders = null;
        public RefTypes Refs = RefTypes.Both;
        public string HintPath = null;
        public string RootFolder = null;
        public List<string> Ignore = null;
        public bool Overwrite = false;
        public SolutionTypes Solution = SolutionTypes.Flat;
    }
}
