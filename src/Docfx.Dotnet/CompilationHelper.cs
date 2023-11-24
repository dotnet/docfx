// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Exceptions;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Docfx.Dotnet;

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

    public static bool CheckDiagnostics(this Compilation compilation, bool errorAsWarning)
    {
        var errorCount = 0;

        foreach (var diagnostic in compilation.GetDeclarationDiagnostics())
        {
            if (diagnostic.IsSuppressed)
                continue;

            if (diagnostic.Severity is DiagnosticSeverity.Warning ||
                (diagnostic.Severity is DiagnosticSeverity.Error && errorAsWarning))
            {
                Logger.LogWarning(diagnostic.ToString());
                continue;
            }

            if (diagnostic.Severity is DiagnosticSeverity.Error)
            {
                Logger.LogError(diagnostic.ToString());

                if (++errorCount >= 20)
                    break;
            }
        }

        return errorCount > 0;
    }

    public static Compilation CreateCompilationFromCSharpFiles(IEnumerable<string> files)
    {
        return CS.CSharpCompilation.Create(
            assemblyName: null,
            options: new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, xmlReferenceResolver: XmlFileResolver.Default),
            syntaxTrees: files.Select(path => CS.SyntaxFactory.ParseSyntaxTree(File.ReadAllText(path), path: path)),
            references: GetDefaultMetadataReferences("C#"));
    }

    public static Compilation CreateCompilationFromCSharpCode(string code, string name = null, params MetadataReference[] references)
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
            assemblyName: null,
            options: new VB.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, globalImports: GetVBGlobalImports(), xmlReferenceResolver: XmlFileResolver.Default),
            syntaxTrees: files.Select(path => VB.SyntaxFactory.ParseSyntaxTree(File.ReadAllText(path), path: path)),
            references: GetDefaultMetadataReferences("VB"));
    }

    public static Compilation CreateCompilationFromVBCode(string code, string name = null, params MetadataReference[] references)
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
            assemblyName: null,
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
            Logger.LogVerbose($"Using SDK reference assemblies in {path}");
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
                    Logger.LogWarning($"Unable to resolve assembly reference {reference}", code: "InvalidAssemblyReference");
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
