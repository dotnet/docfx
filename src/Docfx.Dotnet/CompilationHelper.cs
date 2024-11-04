// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Docfx.Common;
using Docfx.Exceptions;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

#nullable enable

namespace Docfx.Dotnet;

internal static class CompilationHelper
{
    // Bootstrap code to ensure essential types like `System.Object` is loaded for assemblies
    private static readonly SyntaxTree[] s_assemblyBootstrap =
    [
        CS.SyntaxFactory.ParseSyntaxTree(
            """
            class Bootstrap
            {
                public static void Main(string[] foo) { }
            }
            """),
    ];

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

    public static Compilation CreateCompilationFromCSharpFiles(IEnumerable<string> files, IDictionary<string, string> msbuildProperties, MetadataReference[] references)
    {
        var parserOption = GetCSharpParseOptions(msbuildProperties);
        var syntaxTrees = files.Select(path => CS.CSharpSyntaxTree.ParseText(File.ReadAllText(path), parserOption, path: path));

        return CS.CSharpCompilation.Create(
            assemblyName: null,
            options: GetCSharpCompilationOptions(msbuildProperties),
            syntaxTrees: syntaxTrees,
            references: GetDefaultMetadataReferences("C#").Concat(references));
    }

    public static Compilation CreateCompilationFromCSharpCode(string code, IDictionary<string, string> msbuildProperties, string? name = null, params MetadataReference[] references)
    {
        var parserOption = GetCSharpParseOptions(msbuildProperties);
        var syntaxTree = CS.CSharpSyntaxTree.ParseText(code, parserOption);

        return CS.CSharpCompilation.Create(
            name,
            options: GetCSharpCompilationOptions(msbuildProperties),
            syntaxTrees: [syntaxTree],
            references: GetDefaultMetadataReferences("C#").Concat(references ?? []));
    }

    public static Compilation CreateCompilationFromVBFiles(IEnumerable<string> files, IDictionary<string, string> msbuildProperties, MetadataReference[] references)
    {
        var parserOption = GetVisualBasicParseOptions(msbuildProperties);
        var syntaxTrees = files.Select(path => VB.VisualBasicSyntaxTree.ParseText(File.ReadAllText(path), parserOption, path: path));

        return VB.VisualBasicCompilation.Create(
            assemblyName: null,
            options: GetVisualBasicCompilationOptions(msbuildProperties),
            syntaxTrees: syntaxTrees,
            references: GetDefaultMetadataReferences("VB").Concat(references));
    }

    public static Compilation CreateCompilationFromVBCode(string code, IDictionary<string, string> msbuildProperties, string? name = null, params MetadataReference[] references)
    {
        var parserOption = GetVisualBasicParseOptions(msbuildProperties);
        var syntaxTree = VB.VisualBasicSyntaxTree.ParseText(code, parserOption);

        return VB.VisualBasicCompilation.Create(
            name,
            options: GetVisualBasicCompilationOptions(msbuildProperties),
            syntaxTrees: [syntaxTree],
            references: GetDefaultMetadataReferences("VB").Concat(references ?? []));
    }

    public static (Compilation, IAssemblySymbol) CreateCompilationFromAssembly(string assemblyPath, bool includePrivateMembers = false, params MetadataReference[] references)
    {
        var metadataReference = CreateMetadataReference(assemblyPath);
        var compilation = CS.CSharpCompilation.Create(
            assemblyName: null,
            options: new CS.CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                metadataImportOptions: includePrivateMembers
                    ? MetadataImportOptions.All
                    : MetadataImportOptions.Public
            ),
            syntaxTrees: s_assemblyBootstrap,
            references: GetReferenceAssemblies(assemblyPath)
                .Select(CreateMetadataReference)
                .Concat(references ?? [])
                .Append(metadataReference));

        var assembly = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(metadataReference)!;
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
            // Get current .NET runtime version with `{major}.{minor}` format.
            var dotnetMajorMinorVersion = Environment.Version.ToString(2);

            // Resolve .NET SDK packs directory path. (e.g. `C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.2\ref\net8.0`)
            var dotnetExeDirectory = DotNetCorePathFinder.FindDotNetExeDirectory();
            var refDirectory = Path.Combine(dotnetExeDirectory, "packs/Microsoft.NETCore.App.Ref");
            var version = new DirectoryInfo(refDirectory).GetDirectories().Select(d => d.Name).Where(x => x.StartsWith(dotnetMajorMinorVersion)).Max()!;
            var moniker = new DirectoryInfo(Path.Combine(refDirectory, version, "ref")).GetDirectories().Select(d => d.Name).Where(x => x.EndsWith(dotnetMajorMinorVersion)).Max()!;
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
                    // Skip warning for some weired assembly references: https://github.com/dotnet/docfx/issues/9459
                    if (reference.Version?.ToString() != "0.0.0.0")
                    {
                        Logger.LogWarning($"Unable to resolve assembly reference {reference}", code: "InvalidAssemblyReference");
                    }

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

    private static CS.CSharpParseOptions GetCSharpParseOptions(IDictionary<string, string> msbuildProperties)
    {
        var preprocessorSymbols = msbuildProperties.TryGetValue("DefineConstants", out var defineConstants)
            ? defineConstants.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : null;

        return new CS.CSharpParseOptions(preprocessorSymbols: preprocessorSymbols);
    }

    private static VB.VisualBasicParseOptions GetVisualBasicParseOptions(IDictionary<string, string> msbuildProperties)
    {
        IEnumerable<KeyValuePair<string, object>>? preprocessorSymbols;
        if (msbuildProperties.TryGetValue("DefineConstants", out var defineConstants))
        {
            // Visual Basic use symbol/value pairs that are separated by semicolons. And are `key = value` pair syntax:
            // https://learn.microsoft.com/en-us/visualstudio/msbuild/vbc-task?view=vs-2022
            var items = defineConstants.Split(';');
            preprocessorSymbols = items.Select(x => x.Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                       .Where(x => x.Length == 2) // Silently ignore invalid formatted item.
                                       .Select(x => new KeyValuePair<string, object>(x[0].Trim(), x[1].Trim()))
                                       .ToArray();
        }
        else
        {
            preprocessorSymbols = null;
        }

        return new VB.VisualBasicParseOptions(preprocessorSymbols: preprocessorSymbols);
    }

    private static CS.CSharpCompilationOptions GetCSharpCompilationOptions(IDictionary<string, string> msbuildProperties)
    {
        var options = new CS.CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            xmlReferenceResolver: XmlFileResolver.Default);

        if (msbuildProperties.TryGetValue("AllowUnsafeBlocks", out var valueText) && bool.TryParse(valueText, out var allowUnsafe))
        {
            options = options.WithAllowUnsafe(allowUnsafe);
        }

        return options;
    }

    private static VB.VisualBasicCompilationOptions GetVisualBasicCompilationOptions(IDictionary<string, string> msbuildProperties)
    {
        return new VB.VisualBasicCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            globalImports: GetVBGlobalImports(),
            xmlReferenceResolver: XmlFileResolver.Default);
    }
}
