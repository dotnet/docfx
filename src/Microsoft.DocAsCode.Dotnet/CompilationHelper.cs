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

    internal static class CompilationHelper
    {
        // Bootstrap code to ensure essential types like `System.Object` is loaded for assemblies
        private static readonly SyntaxTree[] s_assemblyBootstrap = new[]
        {
            CS.SyntaxFactory.ParseSyntaxTree(
                """
                class Bootstrap
                {
                    public static void Main(string[] foo) { }
                }
                """),
        };

        public static void LogDeclarationDiagnostics(this Compilation compilation)
        {
            foreach (var diagnostic in compilation.GetDeclarationDiagnostics())
            {
                if (diagnostic.IsSuppressed || IsKnownError(diagnostic))
                    continue;

                var level = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Error => LogLevel.Error,
                    DiagnosticSeverity.Warning => LogLevel.Warning,
                    DiagnosticSeverity.Info => LogLevel.Info,
                    _ => LogLevel.Verbose,
                };

                Logger.Log(level, $"{diagnostic}");
            }

            static bool IsKnownError(Diagnostic diagnostic)
            {
                // Ignore these VB errors on non-Windows platform:
                //   error BC30002: Type 'Global.Microsoft.VisualBasic.Devices.Computer' is not defined.
                //   error BC30002: Type 'Global.Microsoft.VisualBasic.ApplicationServices.ApplicationBase' is not defined.
                //   error BC30002: Type 'Global.Microsoft.VisualBasic.MyServices.Internal.ContextValue' is not defined.
                //   error BC30002: Type 'Global.Microsoft.VisualBasic.ApplicationServices.User' is not defined.
                //   error BC30002: Type 'Global.Microsoft.VisualBasic.ApplicationServices.User' is not defined.
                if (!OperatingSystem.IsWindows() && diagnostic.Id == "BC30002" &&
                    diagnostic.GetMessage().Contains("Global.Microsoft.VisualBasic."))
                {
                    return true;
                }

                return false;
            }
        }

        public static Compilation CreateCompilationFromCSharpFiles(IEnumerable<string> files)
        {
            return CS.CSharpCompilation.Create(
                assemblyName: "cs.temp.dll",
                options: new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, xmlReferenceResolver: XmlFileResolver.Default),
                syntaxTrees: files.Select(path => CS.SyntaxFactory.ParseSyntaxTree(File.ReadAllText(path), path: path)),
                references: GetDefaultMetadataReferences("C#"));
        }

        public static Compilation CreateCompilationFromCSharpCode(string code, string name = "cs.temp.dll", params MetadataReference[] references)
        {
            return CS.CSharpCompilation.Create(
                name,
                options: new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, xmlReferenceResolver: XmlFileResolver.Default),
                syntaxTrees: new[] { CS.SyntaxFactory.ParseSyntaxTree(code) },
                references: GetDefaultMetadataReferences("C#").Concat(references));
        }

        public static Compilation CreateCompilationFromVBFiles(IEnumerable<string> files)
        {
            return VB.VisualBasicCompilation.Create(
                assemblyName: "vb.temp.dll",
                options: new VB.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, globalImports: GetVBGlobalImports(), xmlReferenceResolver: XmlFileResolver.Default),
                syntaxTrees: files.Select(path => VB.SyntaxFactory.ParseSyntaxTree(File.ReadAllText(path), path: path)),
                references: GetDefaultMetadataReferences("VB"));
        }

        public static Compilation CreateCompilationFromVBCode(string code, string name = "vb.temp.dll", params MetadataReference[] references)
        {
            return VB.VisualBasicCompilation.Create(
                name,
                options: new VB.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, globalImports: GetVBGlobalImports(), xmlReferenceResolver: XmlFileResolver.Default),
                syntaxTrees: new[] { VB.SyntaxFactory.ParseSyntaxTree(code) },
                references: GetDefaultMetadataReferences("VB").Concat(references));
        }

        public static (Compilation, IAssemblySymbol) CreateCompilationFromAssembly(string assemblyPath, IEnumerable<string> references = null)
        {
            var metadataReference = CreateMetadataReference(assemblyPath);
            var compilation = CS.CSharpCompilation.Create(
                "EmptyProjectWithAssembly",
                options: new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: s_assemblyBootstrap,
                references: GetReferenceAssemblies(assemblyPath)
                    .Concat(references ?? Enumerable.Empty<string>())
                    .Select(CreateMetadataReference)
                    .Append(metadataReference));

            var assembly = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(metadataReference);
            return (compilation, assembly);
        }

        private static IEnumerable<VB.GlobalImport> GetVBGlobalImports()
        {
            // See default global imports in project properties panel for a default VB classlib.
            return VB.GlobalImport.Parse(
                "Microsoft.VisualBasic",
                "System",
                "System.Collections",
                "System.Collections.Generic",
                "System.Diagnostics",
                "System.Linq",
                "System.Xml.Linq",
                "System.Threading.Tasks");
        }

        private static IEnumerable<MetadataReference> GetDefaultMetadataReferences(string language)
        {
            try
            {
                var dotnetExeDirectory = DotNetCorePathFinder.FindDotNetExeDirectory();
                var refDirectory = Path.Combine(dotnetExeDirectory, "packs/Microsoft.NETCore.App.Ref");
                var version = new DirectoryInfo(refDirectory).GetDirectories().Select(d => d.Name).Max();
                var moniker = new DirectoryInfo(Path.Combine(refDirectory, version, "ref")).GetDirectories().Select(d => d.Name).Max();
                var path = Path.Combine(refDirectory, version, "ref", moniker);

                Logger.LogInfo($"Compiling {language} files using .NET SDK {version} for {moniker}");
                Logger.LogVerbose($"Reference assembly directory {path}");
                return Directory.EnumerateFiles(path, "*.dll", SearchOption.TopDirectoryOnly)
                                .Select(CreateMetadataReference);
            }
            catch (Exception ex)
            {
                Logger.LogVerbose(ex.ToString());
                throw new DocfxException("Cannot find .NET Core SDK to compile the project.");
            }
        }

        private static IEnumerable<string> GetReferenceAssemblies(string assemblyPath)
        {
            using var assembly = new PEFile(assemblyPath);
            var assemblyResolver = new UniversalAssemblyResolver(assemblyPath, false, assembly.DetectTargetFrameworkId());
            var result = new Dictionary<string, string>();

            GetReferenceAssembliesCore(assembly);

            void GetReferenceAssembliesCore(PEFile assembly)
            {
                foreach (var reference in assembly.AssemblyReferences)
                {
                    var file = assemblyResolver.FindAssemblyFile(reference);
                    if (file is null)
                    {
                        Logger.LogWarning($"Unable to resolve assembly reference {reference}");
                        continue;
                    }

                    Logger.LogVerbose($"Loaded {reference.Name} from {file}");

                    using var referenceAssembly = new PEFile(file);
                    if (result.TryAdd(referenceAssembly.Name, file))
                    {
                        GetReferenceAssembliesCore(referenceAssembly);
                    }
                }
            }

            return result.Values;
        }

        private static MetadataReference CreateMetadataReference(string assemblyPath)
        {
            var documentation = XmlDocumentationProvider.CreateFromFile(Path.ChangeExtension(assemblyPath, ".xml"));
            return MetadataReference.CreateFromFile(assemblyPath, documentation: documentation);
        }
    }
}
