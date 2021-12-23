using System;
using Mono.Cecil;

namespace Mono.Documentation.Updater.Frameworks
{
    public class MDocMetadataResolver : MetadataResolver
    {
        public MDocMetadataResolver (CachedResolver r) : base (r) { }

        ReaderParameters parameters = new ReaderParameters ();

        public override TypeDefinition Resolve (TypeReference type)
        {
            type = type.GetElementType ();

            var scope = type.Scope;

            if (scope == null)
                return null;
            
            CachedResolver assembly_resolver = this.AssemblyResolver as CachedResolver;
            parameters.AssemblyResolver = assembly_resolver;
            parameters.MetadataResolver = this;

            try {

            switch (scope.MetadataScopeType)
            {
                case MetadataScopeType.AssemblyNameReference:
                    var assembly = assembly_resolver.ResolveCore ((AssemblyNameReference)scope, parameters, type);
                    if (assembly == null)
                        return null;

                    return GetType (assembly.MainModule, type);
                case MetadataScopeType.ModuleDefinition:
                    return GetType ((ModuleDefinition)scope, type);
                case MetadataScopeType.ModuleReference:
                    var modules = type.Module.Assembly.Modules;
                    var module_ref = (ModuleReference)scope;
                    for (int i = 0; i < modules.Count; i++)
                    {
                        var netmodule = modules[i];
                        if (netmodule.Name == module_ref.Name)
                            return GetType (netmodule, type);
                    }
                    break;
            }
            }
            catch(AssemblyResolutionException are)
            {
                throw new MDocException ($"Failed to resolve type '{type.FullName}'", are);
            }
            catch
            {
                throw;
            }

            throw new NotSupportedException ($"metadata scope type {scope.MetadataScopeType.ToString("G")} is not supported");
        }
        static TypeDefinition GetType (ModuleDefinition module, TypeReference reference)
        {
            var type = GetTypeDefinition (module, reference);

            if (type != null)
                return type;

            if (!module.HasExportedTypes)
                return null;

            var exported_types = module.ExportedTypes;

            for (int i = 0; i < exported_types.Count; i++)
            {
                var exported_type = exported_types[i];
                if (exported_type.Name != reference.Name)
                    continue;

                if (exported_type.Namespace != reference.Namespace)
                    continue;
                else
                {
                    var exportNs = ExportTypeFindSourceNs(exported_type);
                    var refNs = ReferenceTypeFindSourceNs(reference);
                    if (exportNs != refNs)
                        continue;
                }

                return exported_type.Resolve ();
            }

            Console.WriteLine(string.Format("Unable to get type {0} in module {1}({2}) from assembly {3}({4})",
                reference.FullName, reference.Module.Name, reference.Module.FileName, module.Assembly.FullName, module.Assembly.MainModule.FileName));
            return null;
        }

        static TypeDefinition GetTypeDefinition (ModuleDefinition module, TypeReference type)
        {
            if (!type.IsNested)
                return module.GetType (type.Namespace, type.Name);

            var declaring_type = type.DeclaringType.Resolve ();
            if (declaring_type == null)
                return null;

            return GetNestedType (declaring_type, TypeFullName(type));
        }

        public static TypeDefinition GetNestedType (TypeDefinition self, string fullname)
        {
            if (!self.HasNestedTypes)
                return null;

            var nested_types = self.NestedTypes;

            for (int i = 0; i < nested_types.Count; i++)
            {
                var nested_type = nested_types[i];

                if (TypeFullName(nested_type) == fullname)
                    return nested_type;
            }

            return null;
        }
        public static string TypeFullName (TypeReference self)
        {
            var sourseNs = ReferenceTypeFindSourceNs(self);
            return string.IsNullOrEmpty(sourseNs)
                ? self.Name
                : sourseNs + '.' + self.Name;
        }

        public static string ExportTypeFindSourceNs(ExportedType type)
        {
            return type.IsForwarder == false ?
                 ExportTypeFindSourceNs(type.DeclaringType) : type.Namespace ?? "";

        }

        public static string ReferenceTypeFindSourceNs(TypeReference type)
        {
            return type.DeclaringType == null ?
                 type.Namespace ?? "" : ReferenceTypeFindSourceNs(type.DeclaringType);
        }
    }
}
