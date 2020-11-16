using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityStubDecompiler
{
    public class SolutionGenerator
    {
        public static void Generate(Options options, IEnumerable<DecompileModule> modules)
        {
            var projects = modules.Select(m => m.Module.AssemblyName);
            WriteSolution(options, projects);
            foreach (var project in projects)
            {
                WriteProject(options, project);
            }
        }
        private static void WriteSolution(Options options, IEnumerable<string> projects)
        {
            using var sw = new StreamWriter($"{options.SolutionDirectoryName}/{options.SolutionDirectoryName}.sln");
            sw.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            sw.WriteLine("# Visual Studio 15");
            sw.WriteLine("VisualStudioVersion = 15.0.28010.2046");
            var projectLookup = projects.ToDictionary(p => p, p => Guid.NewGuid().ToString());
            foreach (var kv in projectLookup) {
                var project = kv.Key;
                var guid = kv.Value;
                sw.WriteLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{project}\", \"{project}\\{project}.csproj\", \"{{{guid}}}\"");
                sw.WriteLine("EndProject");
            }
            var configs = new string[] { "Debug|Any CPU", "Release|Any CPU" };
            sw.WriteLine("Global");
            sw.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            foreach(var config in configs)
            {
                sw.WriteLine($"\t\t{config} = {config}");
            }
            sw.WriteLine("\tEndGlobalSection");
            sw.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (var kv in projectLookup)
            {
                var project = kv.Key;
                var guid = kv.Value;
                foreach (var config in configs)
                {
                    sw.WriteLine($"\t\t{{{guid}}}.{config}.ActiveCfg = {config}");
                    sw.WriteLine($"\t\t{{{guid}}}.{config}.Build.0 = {config}");
                }
            }
            sw.WriteLine("\tEndGlobalSection");
            sw.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
            sw.WriteLine("\t\tHideSolutionNode = FALSE");
            sw.WriteLine("\tGlobalSection(ExtensibilityGlobals) = postSolution");
            sw.WriteLine($"\t\tSolutionGuid = {{{Guid.NewGuid()}}}");
            sw.WriteLine("\tEndGlobalSection");
            sw.WriteLine("EndGlobal");

        }
        private static void WriteProject(Options options, string project)
        {
            using var sw = new StreamWriter($"{options.SolutionDirectoryName}/{project}/{project}.csproj");
            sw.WriteLine(@"<Project Sdk=""Microsoft.NET.Sdk"">");
            sw.WriteLine(@"  <PropertyGroup>");
            sw.WriteLine(@"    <TargetFramework>net35</TargetFramework>");
            sw.WriteLine(@"  </PropertyGroup>");
            sw.WriteLine(@"</Project>");
        }
    }
}
