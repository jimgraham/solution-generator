#region Copyright MIT License
/*
 * Copyright © 2008 François St-Arnaud and John Wood
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * SolGen, Visual Studio Solution Generator for C# Projects (http://codeproject.com/SolGen)
 * Based on original work by John Wood developer of Priorganizer (http://www.priorganizer.com)
 * Adapted for Visual Studio 2005 by François St-Arnaud (francois.starnaud@videotron.ca)
 * Published to CodePlex with original author's permission.
 * 
 * Original software copyright notice:
 * © Copyright 2005 J. Wood Software Services. All rights reserved.
 * You are free to modify this source code at will, but please give credit to John Wood
 * if decide to incorporate or redistribute the resultant binary.
 * http://www.servicestuff.com/jwss/page.aspx?page=utility.htm&utility=solgen
 * 
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Build.BuildEngine;

namespace SolGen
{
    class SolutionGenerator
    {

        #region Constants
        private const string ProjectRefs = ".projRefs";
        private const string AssemblyRefs = ".dllRefs";
        private const string CsProjFileExtension = ".csproj";
        private const string SolutionFileExtension = ".sln";
        private const string AssemblyName = "AssemblyName";
        private const string ProjectGuid = "ProjectGuid";
        private const string AllCsProj = "*" + CsProjFileExtension;
        private const string AllFiles = "*.*";
        private const string Reference = "Reference";
        private const string ProjectReference = "ProjectReference";
        private const string Project = "Project";

        // From Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\8.0\Projects\
        private const string CsProjGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        private const string FolderGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
        
        #endregion

        #region Enums

        #endregion

        #region Members
        private readonly IDictionary<string, ProjectInfo> _projectsByName = new Dictionary<string, ProjectInfo>();
        private readonly IDictionary<string, ProjectInfo> _projectsByFile = new Dictionary<string, ProjectInfo>();
        private readonly IDictionary<string, ProjectInfo> _projectsByGuid = new Dictionary<string, ProjectInfo>();
        private readonly IDictionary<string, ProjectInfo> _foldersByPath  = new Dictionary<string, ProjectInfo>();
        private readonly IList<ProjectInfo> _assemblyRefProjectFilesHandled = new List<ProjectInfo>();
        private readonly IList<ProjectInfo> _projectRefProjectFilesHandled = new List<ProjectInfo>();
        private readonly IList<string> _projectFiles = new List<string>();
        private readonly Engine m_Engine = new Engine();

        private Arguments _arguments = new Arguments();

        #endregion

        #region Private sub-classes




        #endregion

        #region Private methods

        #region Command-line arguments handling
        private static void Header()
        {
            Console.WriteLine("SolGen: C# Solution Generator, Version " + Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("Copyright © 2008 François St-Arnaud and John Wood under MIT License.");
            Console.WriteLine("For more information, see http://codeplex.com/SolGen");
        }

        private static void Usage()
        {
            Console.WriteLine("\nSolGen scans the specified root folder and subfolders looking for");
            Console.WriteLine("C# project files (.csproj). For each project file found, it creates up to 4 files:\n");
            Console.WriteLine("If /refs is set to 'project' or 'both':");
            Console.WriteLine("1) a .projRefs.csproj file which contains project references");
            Console.WriteLine("2) a .projRefs.sln file that contains the current project and all referenced projects\n");
            Console.WriteLine("If /refs is set to 'assembly' or 'both':");
            Console.WriteLine("3) a .dllRefs.csproj file that contains assembly references");
            Console.WriteLine("4) a .dllRefs.sln file that contains only .dllRefs.csproj\n");
            Console.WriteLine("Also, SolGen creates top-level solution files for each subfolder of the specified root");
            Console.WriteLine("until a .csproj file is found. A top-level solution file is a .projRefs.sln file that");
            Console.WriteLine("contains all created .projRefs.csproj files below the root folder.\n");
            Console.WriteLine("Usage: solgen [switches]");
            Console.WriteLine(" /root:folder, the root folder where to start looking for projects");
            Console.WriteLine("</refs:[assembly|project|both]>, optional, default: 'both'");
            Console.WriteLine("</ignore:folder,(...)>, optional, the list of sub-folders to ignore (e.g.: .svn)");
            Console.WriteLine("</ignorePattern:match,(...)>, optional, the list of text matches in project names to ignore (e.g.: Test)");
            Console.WriteLine("</probe:folder,(...)>, optional, additional folders where to look for referenced projects\n");
            Console.WriteLine("</overwrite>, optional, when 'refs' is 'assembly' or 'project', overwrite existing files\n");
            Console.WriteLine("</solution:[flat|deep]>, optional, organize projects in folder structure, default: 'flat'\n");
        }

        private static void GetArg(string line, string prefix, ref string output)
        {
            if (line.ToUpper().StartsWith(prefix.ToUpper()))
            {
                output = line.Substring(prefix.Length);
            }
        }

        //TODO: JAG move to argument
        private static bool ParseArguments(ICollection<string> args, out Arguments arguments)
        {
            string probe = null;
            string refs = null;
            string ignore = null;
            string ignorePattern = null;
            string overwrite = null;
            string solution = null;

            arguments = new Arguments();

            if (args.Count == 0)
            {
                Usage();
                return false;
            }

            foreach (var str in args)
            {
                GetArg(str, "/probe:", ref probe);
                GetArg(str, "/hintpath:", ref arguments.HintPath);
                GetArg(str, "/root:", ref arguments.RootFolder);
                GetArg(str, "/ignore:", ref ignore);
                GetArg(str, "/ignorePattern:", ref ignorePattern);
                GetArg(str, "/refs:", ref refs);
                GetArg(str, "/overwrite", ref overwrite);
                GetArg(str, "/solution:", ref solution);
            }

            if (arguments.RootFolder == null)
            {
                Usage();
                return false;
            }

            if (!Directory.Exists(arguments.RootFolder))
            {
                Console.WriteLine("\nSpecified root folder does not exists.");
                Console.WriteLine(arguments.RootFolder);
                return false;
            }

            arguments.RootFolder = Path.GetFullPath(arguments.RootFolder);

            if (ignore != null)
            {
                var ignores = ignore.Split(new[] { ';', ',' });
                arguments.Ignore = new List<string>(Array.ConvertAll(ignores, Path.GetFullPath));
            }
            arguments.IgnorePattern = new List<string>();
            if (ignorePattern != null)
            {
                arguments.IgnorePattern = ignorePattern.Split(new[] { ';', ',' }).Select(s => s.Trim()).ToList();
            }

            if (overwrite != null)
            {
                if (overwrite != String.Empty)
                {
                    Console.WriteLine("\nOverwrite argument is binary.");
                    return false;
                }
                arguments.Overwrite = true;
            }

            if (solution != null)
            {
                switch (solution.ToLower())
                {
                    case "flat":
                        arguments.Solution = SolutionTypes.Flat;
                        break;
                    case "deep":
                        arguments.Solution = SolutionTypes.Deep;
                        break;
                    default:
                        Console.WriteLine("\nUnknown /solution switch value " + solution + "!.");
                        return false;
                }
            }

            if (probe != null)
            {
                var probes = probe.Split(new[] { ';', ',' });
                arguments.ProbeFolders = new List<string>(probes);
                foreach (var probeFolder in arguments.ProbeFolders.Where(f => !Directory.Exists(f)))
                {
                    Console.WriteLine("\nSpecified probe folder does not exists.");
                    Console.WriteLine(probeFolder);
                    return false;
                }
            }
            else
            {
                arguments.ProbeFolders = new List<string>();
            }
            arguments.ProbeFolders.Add(arguments.RootFolder);

            if (refs != null)
            {
                switch (refs.ToLower())
                {
                    case "assembly":
                        arguments.Refs = RefTypes.Assembly;
                        break;
                    case "project":
                        arguments.Refs = RefTypes.Project;
                        break;
                    case "both":
                        arguments.Refs = RefTypes.Both;
                        break;
                    default:
                        Console.WriteLine("\nUnknown /refs switch value " + refs + "!.");
                        return false;
                }
            }

            if (arguments.HintPath == null)
            {
                arguments.HintPath = string.Empty;
            }

            return true;
        }

        #endregion

        #region Utility methods

        private static string GetAssemblyNameFromFullyQualifiedName(string fullyQualifiedName)
        {
            var assemblyName = fullyQualifiedName ?? string.Empty;
            if (assemblyName.IndexOf(",") != -1)
            {
                assemblyName = assemblyName.Remove(assemblyName.IndexOf(","));
            }
            return assemblyName;
        }

        private static string GetRelativePath(string fromPath, string toPath)
        {
            var fromDirectories = fromPath.Split(Path.DirectorySeparatorChar);
            var toDirectories = toPath.Split(Path.DirectorySeparatorChar);

            // Get the shortest of the two paths
            var length = fromDirectories.Length < toDirectories.Length
                             ? fromDirectories.Length
                             : toDirectories.Length;

            int lastCommonRoot = -1;
            int index;

            // Find common root
            for (index = 0; index < length; index++)
            {
                if (fromDirectories[index].Equals(toDirectories[index], StringComparison.InvariantCultureIgnoreCase))
                {
                    lastCommonRoot = index;
                }
                else
                {
                    break;
                }
            }

            // If we didn't find a common prefix then abandon
            if (lastCommonRoot == -1)
            {
                return null;
            }

            // Add the required number of "..\" to move up to common root level
            var relativePath = new StringBuilder();
            for (index = lastCommonRoot + 1; index < fromDirectories.Length; index++)
            {
                relativePath.Append(".." + Path.DirectorySeparatorChar);
            }

            // Add on the folders to reach the destination
            for (index = lastCommonRoot + 1; index < toDirectories.Length - 1; index++)
            {
                relativePath.Append(toDirectories[index] + Path.DirectorySeparatorChar);
            }
            relativePath.Append(toDirectories[toDirectories.Length - 1]);

            return relativePath.ToString();
        }

        private static bool IsInIgnoreList(string folder, IEnumerable<string> ignoreList)
        {
            return folder != null && ignoreList != null &&
                   ignoreList.Any(ignore => folder.StartsWith(ignore, StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion

        #region Project and DLL probing and reference handling methods

        /// <summary>
        /// Extracts information from a project file
        /// </summary>
        private void ProcessProject(string folder, string file)
        {
            // ignore projects that match to the IgnorePattern
            if (_arguments.IgnorePattern.Any(pattern => Regex.IsMatch(file, pattern)))
            {
                return;
            }

            // ignore projects that *we* created...
            if ((file.EndsWith(ProjectRefs + CsProjFileExtension)) ||
                (file.EndsWith(AssemblyRefs + CsProjFileExtension)))
            {
                return;
            }

            _projectFiles.Add(file);

            var qualifiedFile = Path.Combine(folder, file);
            Project project = new Project(m_Engine);
            project.Load(qualifiedFile);

            ProjectInfo pinfo = new ProjectInfo
            {
                MsBuildProject = project,
                ProjectName = Path.GetFileNameWithoutExtension(file),
                Filename = qualifiedFile
            };

            foreach (BuildPropertyGroup buildPropertyGroup in project.PropertyGroups)
            {
                foreach (BuildProperty buildProperty in buildPropertyGroup)
                {
                    if (buildProperty.Name == AssemblyName)
                    {
                        pinfo.AssemblyName = buildProperty.Value;
                    }
                    else if (buildProperty.Name == ProjectGuid)
                    {
                        pinfo.ProjectGuid = buildProperty.Value;
                    }
                }

                if ((pinfo.AssemblyName != null) && (pinfo.ProjectGuid != null))
                {
                    break;
                }
            }

            Debug.Assert(pinfo.AssemblyName != null);
            Debug.Assert(pinfo.ProjectGuid != null);

            if (_projectsByName.ContainsKey(pinfo.AssemblyName.ToUpper()))
            {
                ProjectInfo existing = _projectsByName[pinfo.AssemblyName.ToUpper()];

                Console.WriteLine("\nThe project " + pinfo.ProjectName);
                Console.WriteLine("With assembly name " + pinfo.AssemblyName);
                Console.WriteLine("Is defined in more than one project file; using:");
                Console.WriteLine(existing.Filename);
            }
            else
            {
                _projectsByName.Add(pinfo.AssemblyName.ToUpper(), pinfo);
            }

            // only record the project if it's not already there
            if (!_projectsByFile.ContainsKey(pinfo.Filename.ToUpper()))
            {
                _projectsByFile.Add(pinfo.Filename.ToUpper(), pinfo);
            }

            // keep a record of the project keyed by its GUID
            if (!_projectsByGuid.ContainsKey(pinfo.ProjectGuid))
            {
                _projectsByGuid.Add(pinfo.ProjectGuid, pinfo);
            }
            else
            {
                Console.WriteLine("\nProjects with same GUID found ! " + pinfo.ProjectGuid);
                Console.WriteLine(pinfo.Filename);
                Console.WriteLine(_projectsByGuid[pinfo.ProjectGuid].Filename);
            }

        }

        /// <summary>
        /// Locates all projects under the given folder
        /// </summary>
        private void FindAllProjects(string folder)
        {
            foreach (string file in Directory.GetFiles(folder, AllCsProj))
            {
                ProcessProject(folder, file);
            }

            foreach (string subFolder in Directory.GetDirectories(folder, AllFiles))
            {
                if (IsInIgnoreList(subFolder, _arguments.Ignore))
                {
                    continue;
                }
                string newFolder = Path.Combine(folder, subFolder);
                FindAllProjects(newFolder);
            }
        }

        /// <summary>
        /// Retrieves the information on references in the project
        /// </summary>
        private void GatherProjectReferences()
        {
            foreach (ProjectInfo projectInfo in _projectsByName.Values)
            {
                Debug.Assert(projectInfo.MsBuildProject != null);

                foreach (BuildItemGroup buildItemGroup in projectInfo.MsBuildProject.ItemGroups)
                {
                    foreach (BuildItem buildItem in buildItemGroup)
                    {
                        // Add assembly references
                        if (buildItem.Name == Reference)
                        {
                            projectInfo.References.Add(GetAssemblyNameFromFullyQualifiedName(buildItem.Include));
                        }
                        // Add project references
                        else if (buildItem.Name == ProjectReference)
                        {
                            projectInfo.References.Add(buildItem.GetMetadata(Project));
                        }
                    }
                }
            }
        }

        private void CollectRelatedProjects(string projectFile, IDictionary<string, ProjectInfo> projects, 
            IList<string> seenProjects = null)
        {
            if (seenProjects == null)
            {
                seenProjects = new List<string>();
            }

            string upperCaseProjectFile = projectFile.ToUpper();
            Debug.Assert(_projectsByFile.ContainsKey(upperCaseProjectFile));

            ProjectInfo pinfo = _projectsByFile[upperCaseProjectFile];

            if (!projects.ContainsKey(pinfo.AssemblyName))
            {
                projects.Add(pinfo.AssemblyName, pinfo);
            }
            if (!seenProjects.Contains(pinfo.AssemblyName))
            {
                seenProjects.Add(pinfo.AssemblyName);
            }

            foreach (string assembly in pinfo.References)
            {
                ProjectInfo reference;
                _projectsByName.TryGetValue(assembly.ToUpper(), out reference);
                if (reference != null)
                {
                    if (seenProjects.Contains(reference.AssemblyName))
                    {
                        seenProjects.Add(reference.AssemblyName);
                        throw new CircularAssemblyReferenceException(seenProjects);
                    }

                    CollectRelatedProjects(reference.Filename, projects, seenProjects);
                }
            }
            seenProjects.Remove(pinfo.AssemblyName);
        }

        private void ResolveProjectGuids()
        {
            foreach (ProjectInfo pinfo in _projectsByName.Values)
            {
                List<string> newReferences = new List<string>();
                foreach (string reference in pinfo.References)
                {
                    string newReference = reference;
                    if (reference.StartsWith("{"))
                    {
                        // must be a GUID...
                        if (_projectsByGuid.ContainsKey(reference))
                        {
                            newReference = _projectsByGuid[reference].AssemblyName;
                        }
                        else
                        {
                            Console.WriteLine("\nReferenced GUID not found !");
                            Console.WriteLine(reference);
                            Console.WriteLine(pinfo.Filename);
                        }
                    }
                    newReferences.Add(newReference);
                }
                pinfo.References = newReferences;
            }
        }

        private void SetFolderParents()
        {
            int rootIndex = _arguments.RootFolder.Length + 1;
            foreach (ProjectInfo projectInfo in _projectsByFile.Values)
            {
                // TODO Substring unsafe; assumes Filename.StartsWith(RootFolder)
                string projectPath = Path.GetDirectoryName(projectInfo.Filename).Substring(rootIndex);
                string[] projParts = projectPath.Split(Path.DirectorySeparatorChar);
                StringBuilder sb = new StringBuilder(_arguments.RootFolder);
                ProjectInfo parent = null;
                for (int projIndex = 0; projIndex < projParts.Length; projIndex++)
                {
                    string folderName = projParts[projIndex];
                    ProjectInfo folderInfo;
                    sb.Append(Path.DirectorySeparatorChar);
                    sb.Append(folderName);
                    _foldersByPath.TryGetValue(sb.ToString(), out folderInfo);
                    if (folderInfo == null)
                    {
                        folderInfo = new ProjectInfo
                        {
                            ProjectGuid = "{" + Guid.NewGuid().ToString().ToUpper() + "}",
                            ProjectName = folderName,
                            Filename = folderName
                        };
                        if (parent != null)
                        {
                            folderInfo.FolderGuid = parent.ProjectGuid;
                        }
                        _foldersByPath.Add(sb.ToString(), folderInfo);
                    }
                    parent = folderInfo;
                }
            }
        }

        #endregion

        #region Project file creation methods

        private void CreateProjectReferenceProjectFile(IDictionary<string, ProjectInfo> projects)
        {
            foreach (var projectInfo in projects.Values.Where(projectInfo => !_projectRefProjectFilesHandled.Contains(projectInfo)))
            {
                Debug.Assert(projectInfo.MsBuildProject != null);

                var assemblyReferencesToMove = new List<BuildItem>();
                BuildItemGroup assemblyReferencesBuildItemGroup = null;
                BuildItemGroup projectReferencesBuildItemGroup = null;
                if ( projectInfo.MsBuildProject.ItemGroups == null ) continue;

                foreach (BuildItemGroup buildItemGroup in projectInfo.MsBuildProject.ItemGroups)
                {
                    foreach (BuildItem buildItem in buildItemGroup)
                    {
                        // Find all assembly references to convert to project references
                        if (buildItem.Name == Reference)
                        {
                            // Keep a bookmark on the BuildItemGroup that holds assembly references
                            assemblyReferencesBuildItemGroup = buildItemGroup;

                            // Take a look at all assembly references to identify those to convert to project references
                            ProjectInfo referencedProjectInfo;
                            var projectName = GetAssemblyNameFromFullyQualifiedName(buildItem.Include);
                            projects.TryGetValue(projectName, out referencedProjectInfo);
                            if (referencedProjectInfo != null)
                            {
                                assemblyReferencesToMove.Add(buildItem);
                            }
                        }

                        if (buildItem.Name == ProjectReference)
                        {
                            // Keep a bookmark on the BuildItemGroup that holds project references
                            projectReferencesBuildItemGroup = buildItemGroup;
                        }
                    }
                }

                // If no assembly references found or none to move, nothing to change; save file as-is
                if (assemblyReferencesBuildItemGroup == null || assemblyReferencesToMove.Count == 0)
                {
                    //Console.WriteLine("Nothing to do.");
                }
                else
                {
                    // If no project reference group exists, create one.
                    if (projectReferencesBuildItemGroup == null)
                    {
                        projectReferencesBuildItemGroup = projectInfo.MsBuildProject.AddNewItemGroup();
                    }

                    // Remove assembly reference and replace by corresponding project reference.
                    foreach (BuildItem buildItem in assemblyReferencesToMove)
                    {
                        assemblyReferencesBuildItemGroup.RemoveItem(buildItem);
                        string replacementAssemblyName = GetAssemblyNameFromFullyQualifiedName(buildItem.Include);
                        var replacementProjectInfo = _projectsByName[replacementAssemblyName.ToUpperInvariant()];
                        string remplacementInclude = GetRelativePath(Path.GetDirectoryName(projectInfo.Filename), replacementProjectInfo.Filename);
                        
                        BuildItem newBuildItem = projectReferencesBuildItemGroup.AddNewItem(ProjectReference, remplacementInclude);
                        newBuildItem.SetMetadata("Project", replacementProjectInfo.ProjectGuid);
                        newBuildItem.SetMetadata("Name", replacementProjectInfo.ProjectName);
                    }
                }

                StringBuilder sb = new StringBuilder(Path.ChangeExtension(projectInfo.Filename, null));
                if (_arguments.Overwrite == false)
                {
                    sb.Append(ProjectRefs);
                }
                sb.Append(CsProjFileExtension);
                string projectFileName = sb.ToString();
                projectInfo.MsBuildProject.Save(projectFileName);
                _projectRefProjectFilesHandled.Add(projectInfo);
            }
        }

        private void CreateAssemblyReferenceProjectFile(IDictionary<string, ProjectInfo> projects)
        {
            foreach (var projectInfo in projects.Values.Where(projectInfo => !_assemblyRefProjectFilesHandled.Contains(projectInfo)))
            {
                Debug.Assert(projectInfo.MsBuildProject != null);

                var projectReferencesToMove = new List<BuildItem>();
                BuildItemGroup assemblyReferencesBuildItemGroup = null;
                BuildItemGroup projectReferencesBuildItemGroup = null;

                if (projectInfo.MsBuildProject.ItemGroups == null) continue;

                foreach (BuildItemGroup buildItemGroup in projectInfo.MsBuildProject.ItemGroups)
                {
                    foreach (BuildItem buildItem in buildItemGroup)
                    {
                        if (buildItem.Name == Reference)
                        {
                            // Keep a bookmark on the BuildItemGroup that holds assembly references
                            assemblyReferencesBuildItemGroup = buildItemGroup;
                        }

                        if (buildItem.Name == ProjectReference)
                        {
                            // Keep a bookmark on the BuildItemGroup that holds project references
                            projectReferencesBuildItemGroup = buildItemGroup;

                            // Take a look at all project references to identify those to convert to assembly references
                            ProjectInfo referencedProjectInfo;
                            var projectGuid = buildItem.GetMetadata(Project);
                            _projectsByGuid.TryGetValue(projectGuid, out referencedProjectInfo);
                            if (referencedProjectInfo != null)
                            {
                                projectReferencesToMove.Add(buildItem);
                            }
                        }
                    }
                }

                // If no project references found or none to move, nothing to change; save file as-is
                if ( projectReferencesBuildItemGroup != null && projectReferencesToMove.Count > 0 )
                {
                    if (assemblyReferencesBuildItemGroup == null)
                    {
                        assemblyReferencesBuildItemGroup = projectInfo.MsBuildProject.AddNewItemGroup();
                    }

                    foreach (var buildItem in projectReferencesToMove)
                    {
                        projectReferencesBuildItemGroup.RemoveItem(buildItem);

                        // Get the assembly name corresponding to the project GUID 
                        foreach (var pinfo in projects.Values)
                        {
                            if (pinfo.ProjectGuid == buildItem.GetMetadata(Project))
                            {
                                var newBuildItem =
                                    assemblyReferencesBuildItemGroup.AddNewItem(Reference, pinfo.AssemblyName);
                                newBuildItem.SetMetadata("Private", bool.FalseString);
                                newBuildItem.SetMetadata("SpecificVersion", bool.FalseString);
                                if (_arguments.HintPath != string.Empty)
                                {
                                    var relative =
                                        GetRelativePath(Path.GetDirectoryName(projectInfo.Filename), _arguments.HintPath);
                                    newBuildItem.SetMetadata("HintPath", Path.Combine(relative, pinfo.AssemblyName) + ".dll");
                                }
                                break;
                            }
                        }
                    }
                }
                StringBuilder sb = new StringBuilder(Path.ChangeExtension(projectInfo.Filename, null));
                if (_arguments.Overwrite == false)
                {
                    sb.Append(AssemblyRefs);
                }
                sb.Append(CsProjFileExtension);
                string projectFileName = sb.ToString();
                projectInfo.MsBuildProject.Save(projectFileName);
                _assemblyRefProjectFilesHandled.Add(projectInfo);
            }
        }

        /// <summary>
        /// Generates a new project file containing only references to project GUIDs
        /// </summary>
        private void CreateProjectFiles(IDictionary<string, ProjectInfo> projects)
        {
            if (_arguments.Refs == RefTypes.Project || _arguments.Refs == RefTypes.Both)
            {
                CreateProjectReferenceProjectFile(projects);
            }

            if (_arguments.Refs == RefTypes.Assembly || _arguments.Refs == RefTypes.Both)
            {
                CreateAssemblyReferenceProjectFile(projects);
            }
        }

        #endregion

        #region Solution file creation methods

        private void WriteSolutionFile(string solutionFile, IDictionary<string, ProjectInfo> projects, RefTypes refType)
        {
            Debug.Assert(solutionFile != null);

            using (var writer = new StreamWriter(solutionFile))
            {
                writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 11.00");
                writer.WriteLine("# Visual Studio 2010");

                // Folders
                if (_arguments.Solution == SolutionTypes.Deep)
                {
                    foreach ( var kvp in _foldersByPath.Where( kvp => kvp.Key.StartsWith(Path.GetDirectoryName(solutionFile) + Path.DirectorySeparatorChar)))
                    {
                        WriteProjectEntry(writer, kvp.Value, true, Path.GetDirectoryName(solutionFile));
                    }
                }

                // Projects
                foreach (var projectInfo in projects.Values)
                {
                    var sb = new StringBuilder(Path.Combine(Path.GetDirectoryName(projectInfo.Filename), Path.GetFileNameWithoutExtension(projectInfo.Filename)));

                    if (_arguments.Overwrite == false)
                    {
                        switch (refType)
                        {
                            case RefTypes.Project:
                                sb.Append(ProjectRefs);
                                break;
                            case RefTypes.Assembly:
                                sb.Append(AssemblyRefs);
                                break;
                        }
                    }
                    sb.Append(CsProjFileExtension);

                    // Create a copy of the project information to avoid modifying the project information in the reference
                    // m_ProjectsByName, m_ProjectsByFile, m_ProjectsByGuid data structures.
                    var projectInfoCopy = projectInfo.ShallowCopy();
                    projectInfoCopy.Filename = sb.ToString();
                    WriteProjectEntry(writer, projectInfoCopy, false, Path.GetDirectoryName(solutionFile));
                }

                // Project and folder relations
                if (_arguments.Solution == SolutionTypes.Deep)
                {
                    writer.WriteLine("Global");
                    writer.WriteLine("\tGlobalSection(NestedProjects) = preSolution");
                    const string format = "\t\t{0} = {1}";

                    // Folder relations
                    // TODO: improve; output only required folder relations
                    foreach (var folderInfo in _foldersByPath.Values.Where(i => i.ProjectGuid != null && i.FolderGuid != null))
                    {
                        writer.WriteLine(format, folderInfo.ProjectGuid, folderInfo.FolderGuid);
                    }

                    // Project relations
                    foreach (var projectInfo in projects.Values)
                    {
                        ProjectInfo folderInfo;
                        _foldersByPath.TryGetValue(Path.GetDirectoryName(projectInfo.Filename), out folderInfo);
                        if (folderInfo != null)
                        {
                            writer.WriteLine(format, projectInfo.ProjectGuid, folderInfo.ProjectGuid);
                        }
                    }

                    writer.WriteLine("\tEndGlobalSection");
                    writer.WriteLine("EndGlobal");
                }
                writer.Close();
            }
        }

        private static void WriteProjectEntry(TextWriter writer, ProjectInfo projectInfo, bool folder, string rootFolder)
        {
            string projectPath;
            string guid;

            if (folder == false)
            {
                // TODO
                projectPath = projectInfo.Filename.StartsWith(rootFolder) 
                    ? projectInfo.Filename.Substring(rootFolder.Length + 1)
                    : projectInfo.Filename;
                guid = CsProjGuid;
            }
            else
            {
                projectPath = projectInfo.ProjectName;
                guid = FolderGuid;
            }

            var format = "Project('{0}') = '{1}', '{2}', '{3}'".Replace('\'', '"');
            writer.WriteLine(format, guid, projectInfo.ProjectName, projectPath, projectInfo.ProjectGuid);
            writer.WriteLine("EndProject");
        }

        private void CreateSolutionFiles(string projectFile, IDictionary<string, ProjectInfo> projects)
        {
            Debug.Assert(projectFile != null);

            // Create single project solution file for project with assembly references
            var singleProject = new Dictionary<string, ProjectInfo>
            {
                {projectFile, _projectsByFile[projectFile.ToUpper()]}
            };

            var sb = new StringBuilder(Path.Combine(Path.GetDirectoryName(projectFile), Path.GetFileNameWithoutExtension(projectFile)));
            if (_arguments.Overwrite == false)
            {
                sb.Append(AssemblyRefs);
            }
            sb.Append(SolutionFileExtension);
            var singleProjectSolutionFile = sb.ToString();

            WriteSolutionFile(singleProjectSolutionFile, singleProject, RefTypes.Assembly);

            // Create "complete" solution with all projects required to fulfill project references
            if (_arguments.Refs == RefTypes.Project || _arguments.Refs == RefTypes.Both)
            {
                var assemblyRefsSolutionFile = Path.Combine(Path.GetDirectoryName(projectFile), Path.GetFileNameWithoutExtension(projectFile)) + ProjectRefs + SolutionFileExtension;
                WriteSolutionFile(assemblyRefsSolutionFile, projects, RefTypes.Project);
            }
        }

        private void CreateTopLevelSolutionFiles(string folder, IDictionary<string, Dictionary<string, ProjectInfo>> allRelatedProjects)
        {
            Debug.Assert(!string.IsNullOrEmpty(folder) && Directory.Exists(folder));

            // No need to create a top-level solution file if root folder contains a .csproj file
            // No need to create a top-level solution file if root folder contains no subfolders
            if (Directory.GetFiles(folder, AllCsProj).Length > 0 || Directory.GetDirectories(folder).Length == 0)
            {
                return;
            }

            // Recursively process all sub-folders of root folder
            foreach (string subFolder in Directory.GetDirectories(folder, AllFiles))
            {
                // Ignore sub-folders in ignore list
                if (IsInIgnoreList(subFolder, _arguments.Ignore))
                {
                    continue;
                }

                // Ignore sub-folders that contain a project file
                if (Directory.GetFiles(subFolder, AllCsProj).Length > 0)
                {
                    continue;
                }

                CreateTopLevelSolutionFiles(subFolder, allRelatedProjects);
            }

            // Create a solution containing a list of all project files under this subfolder
            IDictionary<string, ProjectInfo> projects = new Dictionary<string, ProjectInfo>();
            foreach (KeyValuePair<string, ProjectInfo> kvp in _projectsByFile)
            {
                if (kvp.Key.StartsWith(folder, StringComparison.InvariantCultureIgnoreCase) && !projects.ContainsKey(kvp.Key))
                {
                    projects.Add(kvp.Key, kvp.Value);
                    Dictionary<string, ProjectInfo> relatedProjects;
                    allRelatedProjects.TryGetValue(kvp.Value.Filename, out relatedProjects);
                    if (relatedProjects != null)
                    {
                        foreach (var kvp2 in relatedProjects)
                        {
                            var key = kvp2.Value.Filename.ToUpperInvariant();
                            if (!projects.ContainsKey(key))
                            {
                                projects.Add(key, kvp2.Value);
                            }
                        }
                    }
                }
            }
            if (projects.Count > 0)
            {
                StringBuilder sb = new StringBuilder(folder);
                string[] parts = folder.Split(Path.DirectorySeparatorChar);

                if (parts.Length > 1)
                {
                    sb.Append(Path.DirectorySeparatorChar);
                    sb.Append(parts[parts.Length - 2]);
                }

                sb.Append('.');
                sb.Append(parts[parts.Length - 1]);
  
                if (_arguments.Overwrite == false)
                {
                    sb.Append(ProjectRefs);
                }
                sb.Append(SolutionFileExtension);
                string solutionFileName = sb.ToString();

                WriteSolutionFile(solutionFileName, projects, RefTypes.Project);
            }
        }

        #endregion

        #endregion

        #region Start

        public void Start(string[] args)
        {
            Header();

            if (!ParseArguments(args, out _arguments))
            {
                return;
            }

            Console.WriteLine("\nProbing for projects...");

            // Find all projects under that specified folder
            foreach (string folder in _arguments.ProbeFolders)
            {
                FindAllProjects(folder);
            }

            // Retrieve project references for all loaded projects
            GatherProjectReferences();

            // Resolve any project GUIDs in the project references
            ResolveProjectGuids();

            // Assign GUIDs to all subfolders
            SetFolderParents();

            // For all project files found under the specified folder, create ProjectRefs.csproj and/or
            // AssemblyRefs.csproj file(s) that contain(s) project and/or assembly references.
            Console.WriteLine("\nCreating project and solution files under:");
            Console.WriteLine(_arguments.RootFolder + "...");

            // Keep track of the related projects for generating top-level solutions.
            Dictionary<string, Dictionary<string, ProjectInfo>> allRelatedProjects = new Dictionary<string, Dictionary<string, ProjectInfo>>();

            foreach (string projectFile in _projectFiles)
            {
                string startFolder = Path.Combine(_arguments.RootFolder, " ").Trim().ToUpper();
                if (!projectFile.ToUpper().StartsWith(startFolder))
                {
                    continue;
                }

                Console.WriteLine(projectFile.Replace(_arguments.RootFolder, ""));

                // Find related projects
                Dictionary<string, ProjectInfo> projects = new Dictionary<string, ProjectInfo>();

                CollectRelatedProjects(projectFile, projects);
                allRelatedProjects.Add(projectFile, projects);

                // Create new project file(s)
                CreateProjectFiles(projects);

                // Create solution files
                CreateSolutionFiles(projectFile, projects); 
            }

            // Create top-level solution file
            Console.WriteLine("\nCreating top-level solution files...");
            CreateTopLevelSolutionFiles(_arguments.RootFolder, allRelatedProjects);

            Console.WriteLine("Done.");
        }

        #endregion
    }
}
