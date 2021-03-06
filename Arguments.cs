﻿using System.Collections.Generic;

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
        public List<string> IgnorePattern = null; 
        public bool Overwrite = false;
        public SolutionTypes Solution = SolutionTypes.Flat;
    }
}
