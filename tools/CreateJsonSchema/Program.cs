using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

class Program
{
    static void Main(string[] args)
    {
        var license = Environment.GetEnvironmentVariable("JSON_SCHEMA_LICENSE");
        if (!string.IsNullOrEmpty(license))
        {
            License.RegisterLicense(license);
        }
    }

    static JSchema GenerateJSchema(Type type)
    {
        var generator = new JSchemaGenerator { DefaultRequired = Required.Default };
        var schema = generator.Generate(type, true);

        // If type contains JsonExtensinDataAttribute, additional properties are allowed
        // TODO: Set AllowAdditionalProperties on nested type, not the entry type
        if (!HasJsonExtensionData(type))
        {
            schema.AllowAdditionalProperties = false;
        }
        return schema;
    }

    static bool HasJsonExtensionData(Type type)
    {
        return type.GetProperties().Any(prop => prop.GetCustomAttribute<JsonExtensionDataAttribute>() != null);
    }
}
