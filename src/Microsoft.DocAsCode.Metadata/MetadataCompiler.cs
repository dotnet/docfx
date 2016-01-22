#if !NetCore
namespace Microsoft.DocAsCode.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class MetadataCompiler
    {
        private static readonly ConstructorInfo JsonPropertyAttributeCtor = typeof(JsonPropertyAttribute).GetConstructor(new[] { typeof(string) });
        private static readonly PropertyInfo JsonPropertyAttribute_Required = typeof(JsonPropertyAttribute).GetProperty(nameof(JsonPropertyAttribute.Required));
        private static readonly ConstructorInfo JsonExtensionDataAttributeCtor = typeof(JsonExtensionDataAttribute).GetConstructor(Type.EmptyTypes);
        private static readonly ConstructorInfo DisplayNameAttribute_Ctor = typeof(DisplayNameAttribute).GetConstructor(new[] { typeof(string) });
        private static readonly ConstructorInfo QueryNameAttribute_Ctor = typeof(QueryNameAttribute).GetConstructor(new[] { typeof(string) });

        public Func<string, string> Namer { get; set; }

        public bool ShouldEmitAdditional { get; set; } = true;

        public string NameOfAdditional { get; set; } = "__Additional";

        public Type CollectionType { get; set; } = typeof(List<>);

        public void Compile(IMetadataSchema schema, string assemblyName, string @namespace, string typeName)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }
            if (schema.Definitions == null)
            {
                throw new ArgumentException("There is no definition is schema.", nameof(schema));
            }
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("assemblyName cannot be white space.", nameof(assemblyName));
            }
            if (@namespace == null)
            {
                throw new ArgumentNullException(nameof(@namespace));
            }
            if (string.IsNullOrWhiteSpace(@namespace))
            {
                throw new ArgumentException("namespace cannot be white space.", nameof(@namespace));
            }
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentException("typeName cannot be white space.", nameof(typeName));
            }
            ValidateProperties();
            var file = assemblyName + ".dll";
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);
            var module = assembly.DefineDynamicModule(assemblyName, file);
            CompileCore(schema, module, @namespace, typeName);
            assembly.Save(file);
        }

        public Type Compile(IMetadataSchema schema, ModuleBuilder module, string @namespace, string typeName)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }
            if (schema.Definitions == null)
            {
                throw new ArgumentException("There is no definition is schema.", nameof(schema));
            }
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }
            if (@namespace == null)
            {
                throw new ArgumentNullException(nameof(@namespace));
            }
            if (string.IsNullOrWhiteSpace(@namespace))
            {
                throw new ArgumentException("namespace cannot be white space.", nameof(@namespace));
            }
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentException("typeName cannot be white space.", nameof(typeName));
            }
            ValidateProperties();
            return CompileCore(schema, module, @namespace, typeName);
        }

        public Type CompileCore(IMetadataSchema schema, ModuleBuilder module, string @namespace, string typeName)
        {
            var type = module.DefineType(@namespace + "." + typeName, TypeAttributes.Public);
            int index = 0;
            foreach (var pair in schema.Definitions)
            {
                var pt = GetMetadataType(pair.Value);
                var name = Namer?.Invoke(pair.Key) ?? pair.Key;
                var field = type.DefineField("$field$" + (++index).ToString(), pt, FieldAttributes.Private);
                var prop = type.DefineProperty(name, PropertyAttributes.None, pt, Type.EmptyTypes);
                SetAttributes(pair, prop);
                prop.SetGetMethod(CreateGetMethod(type, pt, name, field));
                prop.SetSetMethod(CreateSetMethod(type, pt, name, field));
            }
            if (ShouldEmitAdditional)
            {
                if (string.IsNullOrWhiteSpace(NameOfAdditional))
                {
                    throw new InvalidOperationException($"{nameof(NameOfAdditional)} cannot be null or whitespace.");
                }
                CreateAdditional(type, ++index);
            }
            return type.CreateType();
        }

        private void ValidateProperties()
        {
            if (ShouldEmitAdditional &&
                string.IsNullOrWhiteSpace(NameOfAdditional))
            {
                throw new InvalidOperationException($"{nameof(NameOfAdditional)} cannot be null or whitespace.");
            }
        }

        private static void SetAttributes(KeyValuePair<string, IMetadataDefinition> pair, PropertyBuilder prop)
        {
            prop.SetCustomAttribute(
                new CustomAttributeBuilder(
                    JsonPropertyAttributeCtor,
                    new object[] { pair.Key },
                    new[] { JsonPropertyAttribute_Required },
                    new object[] { pair.Value.IsRequired ? Required.Always : Required.Default }));

            prop.SetCustomAttribute(
                new CustomAttributeBuilder(
                    DisplayNameAttribute_Ctor,
                    new object[] { pair.Value.DisplayName }));

            if (pair.Value.IsQueryable)
            {
                prop.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        QueryNameAttribute_Ctor,
                        new object[] { pair.Value.QueryName }));
            }
        }

        private Type GetMetadataType(IMetadataDefinition definition)
        {
            var result = GetMetadataTypeCore(definition);
            if (definition.IsMultiValued)
            {
                if (CollectionType == typeof(Array))
                {
                    result = result.MakeArrayType();
                }
                else
                {
                    result = CollectionType.MakeGenericType(result);
                }
            }
            else if (!definition.IsRequired && result.IsValueType)
            {
                result = typeof(Nullable<>).MakeGenericType(result);
            }
            return result;
        }

        private static Type GetMetadataTypeCore(IMetadataDefinition definition)
        {
            switch (definition.Type)
            {
                case "string":
                    return typeof(string);
                case "integer":
                    return typeof(int);
                case "float":
                    return typeof(double);
                case "boolean":
                    return typeof(bool);
                default:
                    throw new NotSupportedException(string.Format("Type '{0}' is not supported.", definition.Type));
            }
        }

        private static MethodBuilder CreateGetMethod(TypeBuilder type, Type pt, string name, FieldBuilder field)
        {
            var getMethod = type.DefineMethod(
                "get_" + name,
                MethodAttributes.NewSlot | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual);
            getMethod.SetParameters(Type.EmptyTypes);
            getMethod.SetReturnType(pt);
            var il = getMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return getMethod;
        }

        private static MethodBuilder CreateSetMethod(TypeBuilder type, Type pt, string name, FieldBuilder field)
        {
            var setMethod = type.DefineMethod(
                "set_" + name,
                MethodAttributes.NewSlot | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual);
            setMethod.SetParameters(new[] { pt });
            setMethod.SetReturnType(typeof(void));
            var il = setMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return setMethod;
        }

        private void CreateAdditional(TypeBuilder type, int index)
        {
            var field = type.DefineField(
                "$field$" + index.ToString(),
                typeof(IDictionary<string, JToken>),
                FieldAttributes.Private);
            var prop = type.DefineProperty(
                NameOfAdditional,
                PropertyAttributes.None,
                typeof(IDictionary<string, JToken>),
                Type.EmptyTypes);
            prop.SetCustomAttribute(
                new CustomAttributeBuilder(
                    JsonExtensionDataAttributeCtor,
                    new object[0]));
            prop.SetGetMethod(
                CreateGetMethod(
                    type,
                    typeof(IDictionary<string, JToken>),
                    NameOfAdditional,
                    field));
            prop.SetSetMethod(
                CreateSetMethod(
                    type,
                    typeof(IDictionary<string, JToken>),
                    NameOfAdditional,
                    field));
        }
    }
}
#endif