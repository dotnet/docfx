using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Mono.Documentation.Util
{
    static class CecilExtensions
    {
        public static string GetDeclaringType (this CustomAttribute attribute)
        {
            var type = attribute.Constructor.DeclaringType;
            var typeName = type.FullName;

            string translatedType = NativeTypeManager.GetTranslatedName (type);
            return translatedType;
        }

        public static IEnumerable<MemberReference> GetMembers (this TypeDefinition type)
        {
            foreach (var c in type.Methods.Where (m => m.IsConstructor))
                yield return (MemberReference)c;
            foreach (var e in type.Events)
                yield return (MemberReference)e;
            foreach (var f in type.Fields)
                yield return (MemberReference)f;
            foreach (var m in type.Methods.Where (m => !m.IsConstructor))
                yield return (MemberReference)m;
            foreach (var t in type.NestedTypes)
                yield return (MemberReference)t;
            foreach (var p in type.Properties)
                yield return (MemberReference)p;
            foreach (var a in type.AttachedEntities())
                yield return (MemberReference)a;
        }

        public static IEnumerable<MemberReference> GetMembers (this TypeDefinition type, string member)
        {
            return GetMembers (type).Where (m => m.Name == member);
        }

        public static MemberReference GetMember (this TypeDefinition type, string member)
        {
            return GetMembers (type, member).EnsureZeroOrOne ();
        }

        public static MemberReference GetMember(this TypeDefinition type, string member, Func<MemberReference, bool> overloadMatchCriteria)
        {
            return GetMembers(type, member)
                .Where(overloadMatchCriteria)
                .FirstOrDefault();
        }

        static T EnsureZeroOrOne<T> (this IEnumerable<T> source)
        {
            if (source.Count () > 1)
                throw new InvalidOperationException ("too many matches");
            return source.FirstOrDefault ();
        }

        public static MethodDefinition GetMethod (this TypeDefinition type, string method)
        {
            return type.Methods
                .Where (m => m.Name == method)
                .EnsureZeroOrOne ();
        }

        public static IEnumerable<MemberReference> GetDefaultMembers (this TypeReference type)
        {
            TypeDefinition def = type as TypeDefinition;
            if (def == null)
                return new MemberReference[0];
            CustomAttribute defMemberAttr = def.CustomAttributes
                    .FirstOrDefault (c => c.AttributeType.FullName == "System.Reflection.DefaultMemberAttribute");
            if (defMemberAttr == null)
                return new MemberReference[0];
            string name = (string)defMemberAttr.ConstructorArguments[0].Value;
            return def.Properties
                    .Where (p => p.Name == name)
                    .Select (p => (MemberReference)p);
        }

        public static IEnumerable<TypeDefinition> GetTypes (this AssemblyDefinition assembly)
        {
            var exportedTypes = assembly.MainModule.ExportedTypes
                                        .Select (et => et.Resolve ())
                                        .Where(e => e != null);

            var types = assembly.Modules.SelectMany (md => md.GetAllTypes ()).Union(exportedTypes);
            return types;
        }

        public static TypeDefinition GetType (this AssemblyDefinition assembly, string type)
        {
            return GetTypes (assembly)
                .Where (td => td.FullName == type)
                .EnsureZeroOrOne ();
        }

        public static bool IsGenericType (this TypeReference type)
        {
            return type.GenericParameters.Count > 0;
        }

        public static bool IsGenericMethod (this MethodReference method)
        {
            return method.GenericParameters.Count > 0;
        }

        public static TypeReference GetUnderlyingType (this TypeDefinition type)
        {
            if (!type.IsEnum)
                return type;
            return type.Fields.First (f => f.Name == "value__").FieldType;
        }

        public static IEnumerable<TypeDefinition> GetAllTypes (this ModuleDefinition self)
        {
            return self.Types.SelectMany (t => t.GetAllTypes ());
        }

        static IEnumerable<TypeDefinition> GetAllTypes (this TypeDefinition self)
        {
            yield return self;

            if (!self.HasNestedTypes)
                yield break;

            foreach (var type in self.NestedTypes.SelectMany (t => t.GetAllTypes ()))
                yield return type;
        }

        public static IEnumerable<MemberReference> AttachedEntities(this TypeDefinition type)
        {
            return AttachedEntitiesHelper.GetAttachedEntities(type);
        }
    }
}