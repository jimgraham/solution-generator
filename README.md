# Solution Generator

This is a fork of the [SolGen](https://solgen.codeplex.com/) project on CodePlex. This fork is simply to get a larger solution (~35 projects) to convert

Below is the original description.

# SolGen Project Description

This utility generates Visual Studio solutions files and modifies C# project files (.csproj) to change assembly references to project references and vice versa.

## Rationale

Since 2005, Visual Studio uses MSBuild and, independantly of the content of the solution file, projects can have project and/or assembly references and they will build correctly. However, "incomplete" solutions (i.e.: solutions that contain projects that have references to other projects which are not themselves in the solution) will cause problems for developers: even though the solution builds, the VS IDE will complain about missing references (those darn yellow triangles!), and things like IntelliSense, auto-completion, internal to VS or via plugins (such as ReSharper) will be broken.

That said, it is still preferable to have project references for the build process to ensure that the project dependencies are taken into consideration. Project dependencies are now driven by the project files, not the solution file (solution files are regenerated on-the-fly by Visual Studio to take into account project dependencies in its project files).

In large projects structured as described as a Partitioned Solution (see below), there are two conflicting needs:

>    developers like to work with small solutions, and often they want to focus on a single project, not open a solution with tons of projects. Hence, they prefer assembly references in their project files. When they open up a project by double-clicking on a .csproj file, they expect only that project to open. However, Visual Studio insists on opening up a solution: if it does not find a solution file in the same folder as the project file, it looks one level up. Builders want to have projects that have project references to take into account project build dependencies.

## Partitioned Solution
Ref.: [Partitioned Solution](http://www.codeplex.com/TFSGuide/Wiki/View.aspx?title=Chapter%203%20-%20Structuring%20Projects%20and%20Solutions%20in%20Source%20Control&referringTitle=Home)

If you work on a large system, consider using multiple solutions, each representing a sub-system in your application. These solutions can be used by developers in order to work on smaller parts of the system without having to load all code across all projects. Design your solution structure so any projects that have dependencies are grouped together. This enables you to use project references rather than file references. Also consider creating a master solution file that contains all of the projects. You can use this to build your entire application."

 > "Unlike previous versions of Visual Studio, Visual Studio 2005 relies upon MSBuild. It is now possible to create solution structures that do not include all referenced projects and still build without errors. As long as the master solution has been built first, generating the binary output from each project, MSBuild is able to follow project references outside the bounds of your solution and build successfully. This only works if you use project references, not file references. You can successfully build solutions created this way from the Visual Studio build command line and from the IDE, but not with Team Build by default. In order to build successfully with Team Build use the master solution that includes all of the projects and dependencies."

> "The main reason not to use this structure is: Increased solution maintenance costs. Adding a new project might require multiple solution file changes."

## Enter SolGen

SolGen caters to both the dev and build needs by converting assembly references to project references (and vice versa) and solves the increased solution maintenance cost of the Partioned Solution structure by generating solution files instead of maintaining them by hand:

SolGen scans the specified root folder and subfolders looking for C# project files (.csproj). For each project file found, it creates up to 4 files:

If /refs is set to 'project' or 'both':

```
    a .projRefs.csproj file which contains project references
    a .projRefs.sln file that contains the current project and all referenced projects
```

If /refs is set to 'assembly' or 'both':

```
    a .dllRefs.csproj file that contains assembly references
    a .dllRefs.sln file that contains only .dllRefs.csproj
```

Also, SolGen creates top-level solution files for each subfolder of the specified root until a .csproj file is found. A top-level solution file is a .projRefs.sln file that contains all created .projRefs.csproj files below the root folder.

## Usage

Usage: `solgen [switches]`
 
```
/root:folder, the root folder where to start looking for projects
</refs:[assembly|project|both]>, optional, default: 'both'
</ignore:folder,(...)>, optional, the list of sub-folders to ignore (e.g.: .svn)
</probe:folder,(...)>, optional, additional folders where to look for referenced projects
</overwrite>, optional, when 'refs' is 'assembly' or 'project', overwrite existing files
</solution:[flat|deep]>, optional, organize projects in folder structure, default: 'flat'
```

## History

Based on original work by John Wood developer of [Priorganizer](http://www.priorganizer.com/)
Adapted for Visual Studio 2005 and 2010 by François St-Arnaud
Published to CodePlex with original author's permission.

Original software copyright notice:

© Copyright 2005 [J. Wood Software Services](http://www.servicestuff.com/jwss/page.aspx?page=utility.htm&utility=solgen). All rights reserved.
You are free to modify this source code at will, but please give credit to John Wood if decide to incorporate or redistribute the resultant binary.