using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DocAsCode.Build.Engine;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Microsoft.DocAsCode.Metadata.ManagedReference;
using Microsoft.DocAsCode.Plugins;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.DocAsCode.Tools.SandcastleRefMapper
{
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand()
            {
                Handler = CommandHandler.Create(new Action<string, Uri, FileInfo>(Execute))
            };
            rootCommand.Argument = new Argument<string>()
            {
                Name = "inputAssemblyPath",
                Description = "Assembly to create Xref Map for"
            }.LegalFilePathsOnly();
            rootCommand.Argument.AddValidator(symbol =>
                                              symbol.Arguments
                                                    .Where(filePath => !File.Exists(filePath))
                                                    .Select(symbol.ValidationMessages.FileDoesNotExist)
                                                    .FirstOrDefault());
            rootCommand.AddOption(new Option("--baseUrl", "", new Argument<Uri>()));
            rootCommand.AddOption(new Option("--outputYamlPath", "", new Argument<FileInfo>().LegalFilePathsOnly()));
            return rootCommand.InvokeAsync(args);
        }

        private static void Execute(string inputAssemblyPath, Uri baseUrl, FileInfo outputYamlPath)
        {
            ExtractMetadataOptions options = new ExtractMetadataOptions();
            AssemblyFileInputParameters parameters = new AssemblyFileInputParameters(options, inputAssemblyPath);
            string[] assemblyPaths = Enumerable.Repeat(inputAssemblyPath, 1).ToArray();
            Compilation compilation = CreateCompilationFromAssembly(assemblyPaths);
            (_, IAssemblySymbol assembly) = GetAssemblyFromAssemblyComplation(compilation, assemblyPaths).First();
            RoslynSourceFileBuildController controller = new RoslynSourceFileBuildController(compilation, assembly);
            MetadataItem metadata = controller.ExtractMetadata(parameters);
            Queue<MetadataItem> metadataItems = new Queue<MetadataItem>();
            metadataItems.Enqueue(metadata);
            XRefMap refMap = new XRefMap()
            {
                BaseUrl = baseUrl.AbsoluteUri,
                References = new List<XRefSpec>()
            };
            while (metadataItems.Count > 0)
            {
                MetadataItem item = metadataItems.Dequeue();
                foreach (MetadataItem metadataItem in item.Items ?? Enumerable.Empty<MetadataItem>())
                {
                    metadataItems.Enqueue(metadataItem);
                }

                switch (item.Type)
                {
                    case MemberType.Assembly:
                    case MemberType.AttachedEvent:
                    case MemberType.AttachedProperty:
                    case MemberType.Toc:
                        continue;
                }

                refMap.References.Add(new XRefSpec()
                {
                    Uid = item.Name,
                    Name = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.Default, null),
                    Href = SandcastleHelper.GetFileName(item.CommentId) + ".htm",
                    CommentId = item.CommentId
                });
            }
            refMap.Sort();
            YamlUtility.Serialize(outputYamlPath.FullName, refMap);
        }

        private static Compilation CreateCompilationFromAssembly(IEnumerable<string> assemblyPaths)
        {
            try
            {
                List<string> paths = assemblyPaths.ToList();
                //TODO: "mscorlib" should be ignored while extracting metadata from .NET Core/.NET Framework
                paths.Add(typeof(object).Assembly.Location);
                List<PortableExecutableReference> assemblies = (from path in paths
                                  select MetadataReference.CreateFromFile(path)).ToList();
                CS.CSharpCompilationOptions options = new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                return CS.CSharpCompilation.Create("EmptyProjectWithAssembly", new SyntaxTree[] { }, assemblies, options);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error generating compilation from assemblies {string.Join(Environment.NewLine, assemblyPaths)}: {e.Message}. Ignored.");
                return null;
            }
        }

        private static IEnumerable<(MetadataReference reference, IAssemblySymbol assembly)> GetAssemblyFromAssemblyComplation(Compilation assemblyCompilation, IReadOnlyCollection<string> assemblyPaths)
        {
            foreach (MetadataReference reference in assemblyCompilation.References)
            {
                Logger.LogVerbose($"Loading assembly {reference.Display}...");
                IAssemblySymbol assembly = (IAssemblySymbol)assemblyCompilation.GetAssemblyOrModuleSymbol(reference);
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
    }

    internal static class SandcastleHelper
    {
        // The reflection file can contain tens of thousands of entries for large assemblies.  HashSet<T> is much
        // faster at lookups than List<T>.
        private static readonly HashSet<string> filenames = new HashSet<string>();

        private static readonly Regex reInvalidChars = new Regex("[ :.`#<>*?|]", RegexOptions.Compiled);

        // Convert a member ID to a filename based on the given naming method
        public static string GetFileName(string id)
        {
            string memberName, newName;
            bool duplicate;
            int idx;

            memberName = id;

            // Remove parameters
            idx = memberName.IndexOf('(');

            if (idx != -1)
            {
                memberName = memberName.Substring(0, idx);
            }

            // Replace invalid filename characters with an underscore if member names are used as the filenames
            newName = memberName = reInvalidChars.Replace(memberName, "_");

            idx = 0;

            do
            {
                // Check for a duplicate (i.e. an overloaded member).  These will be made unique by adding a
                // counter to the end of the name.
                duplicate = filenames.Contains(newName);

                // VS2005/Hana style bug (probably fixed).  Overloads pages sometimes result in a duplicate
                // reflection file entry and we need to ignore it.
                if (duplicate)
                {
                    if (id.StartsWith("Overload:", StringComparison.Ordinal))
                    {
                        duplicate = false;
                    }
                    else
                    {
                        idx++;
                        newName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", memberName, idx);
                    }
                }
            } while (duplicate);

            // Log duplicates that had unique names created
            if (idx != 0)
            {
                Console.WriteLine("    Unique name {0} generated for {1}", newName, id);
            }

            filenames.Add(newName);

            return newName;
        }
    }
}
