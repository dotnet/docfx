// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.Win32;
    using CS = Microsoft.CodeAnalysis.CSharp;
    using VB = Microsoft.CodeAnalysis.VisualBasic;

    internal static class CompilationUtility
    {
        public static Compilation CreateCompilationFromCsharpCode(string code, string name = "cs.temp.dll", params MetadataReference[] references)
        {
            try
            {
                var tree = CS.SyntaxFactory.ParseSyntaxTree(code);
                return CS.CSharpCompilation.Create(
                    name,
                    options: new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    syntaxTrees: new[] { tree },
                    references: GetNetFrameworkMetadataReferences().Concat(references));
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error generating compilation for C# code {GetAbbreviateString(code)}: {e.Message}. Ignored.");
                return null;
            }
        }

        public static Compilation CreateCompilationFromVBCode(string code, string name = "vb.temp.dll", params MetadataReference[] references)
        {
            try
            {
                var tree = VB.SyntaxFactory.ParseSyntaxTree(code);
                return VB.VisualBasicCompilation.Create(
                    name,
                    options: new VB.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    syntaxTrees: new[] { tree },
                    references: GetNetFrameworkMetadataReferences().Concat(references));
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error generating compilation for VB code {GetAbbreviateString(code)}: {e.Message}. Ignored.");
                return null;
            }
        }

        //TODO: Only process CSharp assembly currently
        public static Compilation CreateCompilationFromAssembly(IEnumerable<string> assemblyPaths)
        {
            try
            {
                var options = new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                var references = GetNetFrameworkMetadataReferences().Concat(assemblyPaths.Select(path => MetadataReference.CreateFromFile(path)));
                return CS.CSharpCompilation.Create("EmptyProjectWithAssembly", new SyntaxTree[] { }, references, options);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error generating compilation from assemblies {string.Join(Environment.NewLine, assemblyPaths)}: {e.Message}. Ignored.");
                return null;
            }
        }

        public static IEnumerable<(MetadataReference reference, IAssemblySymbol assembly)> GetAssemblyFromAssemblyComplation(Compilation assemblyCompilation, IReadOnlyCollection<string> assemblyPaths)
        {
            foreach(var reference in assemblyCompilation.References)
            {
                Logger.LogVerbose($"Loading assembly {reference.Display}...");
                var assembly = (IAssemblySymbol)assemblyCompilation.GetAssemblyOrModuleSymbol(reference);
                if (assembly == null)
                {
                    Logger.LogWarning($"Unable to get symbol from {reference.Display}, ignored...");
                }
                else
                {
                    //TODO: "mscorlib" shouldn't be ignored while extracting metadata from .NET Core/.NET Framework
                    if (assembly.Identity?.Name == "mscorlib")
                    {
                        Logger.LogVerbose($"Ignored mscorlib assembly {reference.Display}");
                        continue;
                    }

                    if (reference is PortableExecutableReference portableReference &&
                        assemblyPaths.Contains(portableReference.FilePath))
                    {
                        yield return (reference, assembly);
                    }
                }
            }
        }

        private static string GetAbbreviateString(string input, int length = 20)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= 20) return input;
            return input.Substring(0, length) + "...";
        }

        private static MetadataReference[] GetNetFrameworkMetadataReferences()
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full");
            var installPath = key?.GetValue("InstallPath")?.ToString() ?? throw new DocfxException("Cannot compile project, make sure .NET Framework is installed.");

            return new[]
            {
                MetadataReference.CreateFromFile(Path.Combine(installPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(installPath, "System.dll")),
            };
        }
    }
}
