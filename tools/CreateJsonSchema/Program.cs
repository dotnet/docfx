using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Docs.Build;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var license = Environment.GetEnvironmentVariable("JSON_SCHEMA_LICENSE");
        if (!string.IsNullOrEmpty(license))
        {
            Console.WriteLine("Found JSON schema license");
            License.RegisterLicense(license);
        }

        if (Directory.Exists("schemas"))
        {
            Directory.Delete("schemas", recursive: true);
        }
        Directory.CreateDirectory("schemas");

        GenerateJSchema(typeof(Config), "docfx");
        GenerateJSchema(typeof(TableOfContentsInputModel), "TOC");

        foreach (var type in typeof(PageModel).Assembly.ExportedTypes)
        {
            if (type.GetCustomAttribute<DataSchemaAttribute>() != null)
            {
                GenerateJSchema(type, type.Name);
            }
        }

        var diff = await ProcessUtility.Execute("git", "diff --ignore-all-space --ignore-blank-lines schemas");
        if (!string.IsNullOrEmpty(diff))
        {
            Console.WriteLine("Json schema change detected. Run ./build.ps1 locally and commit these json schema changes:");
            Console.WriteLine("");
            Console.WriteLine(diff);
            return 1;
        }

        return 0;
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

        try
        {
            var schema = generator.Generate(type, rootSchemaNullable: false);

            // If type contains JsonExtensinDataAttribute, additional properties are allowed
            // TODO: Set AllowAdditionalProperties on nested type, not the entry type
            if (!HasJsonExtensionData(type))
            {
                schema.AllowAdditionalProperties = false;
            }

            File.WriteAllText(Path.Combine("schemas", name + ".json"), schema.ToString());
        }
        catch (JSchemaException)
        {
            Console.Error.WriteLine("Json schema rate limit exceeded");
            Environment.Exit(0);
        }
    }

    static bool HasJsonExtensionData(Type type)
    {
        return type.GetProperties().Any(prop => prop.GetCustomAttribute<JsonExtensionDataAttribute>() != null);
    }
}
