using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Docs.Build;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

class Program
{
    static int Main(string[] args)
    {
        var license = Environment.GetEnvironmentVariable("JSON_SCHEMA_LICENSE");
        if (!string.IsNullOrEmpty(license))
        {
            Console.WriteLine("Found JSON schema license");
            License.RegisterLicense(license);
        }

        if (Directory.Exists("schemas"))
        {
            Directory.Delete("schemas");
        }
        Directory.CreateDirectory("schemas");

        GenerateJSchema(typeof(Config));

        foreach (var type in typeof(PageModel).Assembly.ExportedTypes)
        {
            if (type.GetCustomAttribute<DataSchemaAttribute>() != null)
            {
                GenerateJSchema(type);
            }
        }

        var git = Process.Start(new ProcessStartInfo { FileName = "git", Arguments = "status schemas --porcelain", RedirectStandardOutput = true });
        git.WaitForExit();
        var status = git.StandardOutput.ReadToEnd().Trim();
        if (!string.IsNullOrEmpty(status))
        {
            Console.WriteLine("Json schema change detected. Run ./build.ps1 locally and commit these json schema changes:");
            Console.WriteLine("");
            Console.WriteLine(status);
            return 1;
        }

        return 0;
    }

    static void GenerateJSchema(Type type)
    {
        var generator = new JSchemaGenerator
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }, 
            DefaultRequired = Required.Default
        };
        var schema = generator.Generate(type, true);

        // If type contains JsonExtensinDataAttribute, additional properties are allowed
        // TODO: Set AllowAdditionalProperties on nested type, not the entry type
        if (!HasJsonExtensionData(type))
        {
            schema.AllowAdditionalProperties = false;
        }

        File.WriteAllText(Path.Combine("schemas", type.Name + ".json"), schema.ToString());
    }

    static bool HasJsonExtensionData(Type type)
    {
        return type.GetProperties().Any(prop => prop.GetCustomAttribute<JsonExtensionDataAttribute>() != null);
    }
}
