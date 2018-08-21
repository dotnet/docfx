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

        foreach (var (type, name) in schemas.Skip(skip).Take(take))
        {
            GenerateJSchema(type, name);
        }

        if (skip + take >= schemas.Count)
        {
            var diff = await ProcessUtility.Execute("git", "diff --ignore-all-space --ignore-blank-lines schemas");
            if (!string.IsNullOrEmpty(diff))
            {
                Console.WriteLine("Json schema change detected. Run ./build.ps1 locally and commit these json schema changes:");
                Console.WriteLine("");
                Console.WriteLine(diff);
                return 1;
            }
        }
        else
        {
            await ProcessUtility.Execute("dotnet", $"run -p tools/CreateJsonSchema --no-build --no-restore -- {skip + take}", redirectOutput: false);
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

    static void GenerateJSchema(Type type, string name)
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

        var schema = generator.Generate(type, rootSchemaNullable: false);

        // If type contains JsonExtensinDataAttribute, additional properties are allowed
        // TODO: Set AllowAdditionalProperties on nested type, not the entry type
        if (!HasJsonExtensionData(type))
        {
            schema.AllowAdditionalProperties = false;
        }

        File.WriteAllText(Path.Combine("schemas", name + ".json"), schema.ToString());
    }

    static bool HasJsonExtensionData(Type type)
    {
        return type.GetProperties().Any(prop => prop.GetCustomAttribute<JsonExtensionDataAttribute>() != null);
    }
}
