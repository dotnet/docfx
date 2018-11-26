// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;

    using Microsoft.DocAsCode.Common;

    using CS = Microsoft.CodeAnalysis.CSharp;
    using VB = Microsoft.CodeAnalysis.VisualBasic;

    internal static class CompilationUtility
    {
        private static readonly Lazy<MetadataReference> MscorlibMetadataReference = new Lazy<MetadataReference>(() => MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        public static Compilation CreateCompilationFromCsharpCode(string code)
        {
            try
            {
                var tree = CS.SyntaxFactory.ParseSyntaxTree(code);
                var compilation = CS.CSharpCompilation.Create(
                    "cs.temp.dll",
                    options: new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    syntaxTrees: new[] { tree },
                    references: new[] { MscorlibMetadataReference.Value });
                return compilation;
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error generating compilation for C# code {GetAbbreviateString(code)}: {e.Message}. Ignored.");
                return null;
            }
        }

        public static Compilation CreateCompilationFromVBCode(string code)
        {
            try
            {
                var tree = VB.SyntaxFactory.ParseSyntaxTree(code);
                var compilation = VB.VisualBasicCompilation.Create(
                    "vb.temp.dll",
                    options: new VB.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    syntaxTrees: new[] { tree },
                    references: new[] { MscorlibMetadataReference.Value });
                return compilation;
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
                var paths = assemblyPaths.ToList();
                //TODO: "mscorlib" should be ignored while extracting metadata from .NET Core/.NET Framework
                paths.Add(typeof(object).Assembly.Location);
                var assemblies = (from path in paths
                                  select MetadataReference.CreateFromFile(path)).ToList();
                var options = new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                var complilation = CS.CSharpCompilation.Create("EmptyProjectWithAssembly", new SyntaxTree[] { }, assemblies, options);
                return complilation;
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
    }
}
