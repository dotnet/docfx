// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using ICSharpCode.Decompiler.Metadata;
    using Microsoft.CodeAnalysis;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
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
                    references: GetDefaultMetadataReferences().Concat(references));
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
                    references: GetDefaultMetadataReferences().Concat(references));
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error generating compilation for VB code {GetAbbreviateString(code)}: {e.Message}. Ignored.");
                return null;
            }
        }

        public static Compilation CreateCompilationFromAssembly(IEnumerable<string> assemblyPaths, IEnumerable<string> references = null)
        {
            try
            {
                return CS.CSharpCompilation.Create(
                    "EmptyProjectWithAssembly",
                    options: new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    syntaxTrees: Array.Empty<SyntaxTree>(),
                    references: assemblyPaths
                        .Concat(assemblyPaths.SelectMany(GetReferenceAssemblies))
                        .Concat(references ?? Enumerable.Empty<string>())
                        .Select(file => MetadataReference.CreateFromFile(file)));
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error generating compilation from assemblies {string.Join(Environment.NewLine, assemblyPaths)}: {e.Message}. Ignored.");
                return null;
            }
        }

        public static IEnumerable<(MetadataReference reference, IAssemblySymbol assembly)> GetAssemblyFromAssemblyComplation(Compilation assemblyCompilation, IReadOnlyCollection<string> assemblyPaths)
        {
            foreach (var reference in assemblyCompilation.References)
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

        private static IEnumerable<MetadataReference> GetDefaultMetadataReferences()
        {
            try
            {
                var dotnetExeDirectory = DotNetCorePathFinder.FindDotNetExeDirectory();
                var refDirectory = Path.Combine(dotnetExeDirectory, "packs/Microsoft.NETCore.App.Ref");
                var version = new DirectoryInfo(refDirectory).GetDirectories().Select(d => d.Name).Max();
                var moniker = new DirectoryInfo(Path.Combine(refDirectory, version, "ref")).GetDirectories().Select(d => d.Name).Max();
                var path = Path.Combine(refDirectory, version, "ref", moniker);

                Logger.LogInfo($"Compile using .NET SDK {version} for {moniker}");
                return Directory.EnumerateFiles(path, "*.dll", SearchOption.TopDirectoryOnly)
                                .Select(path => MetadataReference.CreateFromFile(path));
            }
            catch
            {
                throw new DocfxException("Cannot find .NET Core SDK to compile the project.");
            }
        }

        private static IEnumerable<string> GetReferenceAssemblies(string assemblyPath)
        {
            using var assembly = new PEFile(assemblyPath);
            var assemblyResolver = new UniversalAssemblyResolver(assemblyPath, false, assembly.DetectTargetFrameworkId());
            foreach (var reference in assembly.AssemblyReferences)
            {
                if (assemblyResolver.FindAssemblyFile(reference) is { } file)
                    yield return file;
            }
        }
    }
}
