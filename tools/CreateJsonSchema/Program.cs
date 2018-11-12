using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Docs.Build;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var schemas = GetSchemas().ToList();
        var skip = args.Length > 0 ? int.Parse(args[0]) : 0;
        var take = 10;

        if (skip == 0)
        {
            if (Directory.Exists("schemas"))
            {
                Directory.Delete("schemas", recursive: true);
            }
            Directory.CreateDirectory("schemas");
        }

        var generator = CreateGenerator();

        foreach (var (type, name) in schemas.Skip(skip).Take(take))
        {
            GenerateJSchema(generator, type, name);
        }

        if (skip + take >= schemas.Count)
        {
            var diff = await ProcessUtility.Execute("git", "diff --ignore-all-space --ignore-blank-lines schemas");
            if (!string.IsNullOrEmpty(diff.stdout))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Json schema change detected. Run ./build.ps1 locally and commit these json schema changes:");
                Console.ResetColor();
                Console.WriteLine("");
                Console.WriteLine(diff.stdout);
                return 1;
            }
        }
        else
        {
            await ProcessUtility.Execute("dotnet", $"run -p tools/CreateJsonSchema --no-build --no-restore -- {skip + take}", stdout: false, stderr: false);
        }
        return 0;
    }

    static IEnumerable<(Type type, string name)> GetSchemas()
    {
        yield return ((typeof(Config), "docfx"));
        yield return ((typeof(TableOfContentsInputModel), "TOC"));

        foreach (var type in typeof(PageModel).Assembly.ExportedTypes)
        {
            if (type.GetCustomAttribute<DataSchemaAttribute>() != null)
            {
                yield return (type, type.Name);
            }
        }
    }

    static void GenerateJSchema(JSchemaGenerator generator, Type type, string name)
    {
        var schema = generator.Generate(type, rootSchemaNullable: false);

        VerifyContract(generator.ContractResolver, type);

        File.WriteAllText(Path.Combine("schemas", name + ".json"), schema.ToString());
    }

    private static void VerifyContract(IContractResolver resolver, Type type, HashSet<Type> set = null)
    {
        set = set ?? new HashSet<Type>();
        if (!set.Add(type))
        {
            return;
        }

        var contract = resolver.ResolveContract(type);
        if (contract is JsonObjectContract objectContract)
        {
            if (!type.IsSealed && objectContract.ExtensionDataGetter == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Seal '{type}' to disable additional properties in JSON schema, or add an `ExtensionData` property marked as [JsonExtensionData]");
                Console.ResetColor();
                Environment.Exit(1);
            }
            foreach (var property in objectContract.Properties)
            {
                VerifyContract(resolver, property.PropertyType, set);
            }
        }
        else if (contract is JsonArrayContract arrayContract)
        {
            VerifyContract(resolver, arrayContract.CollectionItemType, set);
        }
    }

    private static JSchemaGenerator CreateGenerator()
    {
        var generator = new JSchemaGenerator
        {
            DefaultRequired = Required.DisallowNull,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
        };

        generator.GenerationProviders.Add(new StringEnumGenerationProvider { CamelCaseText = true });
        return generator;
    }
}
