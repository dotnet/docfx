using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;
using Mono.Documentation.Updater;

namespace Mono.Documentation.Util
{
    public static class AttachedEntitiesHelper
    {
        private const string PropertyConst = "Property";
        private const string EventConst = "Event";
        
        private static readonly int EventLength = EventConst.Length;
        private static readonly int PropertyLength = PropertyConst.Length;

        public static string GetEventName(string fieldDefinitionName)
        {
            return fieldDefinitionName.Substring(0, fieldDefinitionName.Length - EventLength);
        }

        public static string GetPropertyName(string fieldDefinitionName)
        {
            return fieldDefinitionName.Substring(0, fieldDefinitionName.Length - PropertyLength);
        }

        public static IEnumerable<MemberReference> GetAttachedEntities(TypeDefinition type)
        {
            var methodsLookUpTable = GetMethodsLookUpTable(type);
            foreach (var attachedEventReference in GetAttachedEvents(type, methodsLookUpTable))
            {
                yield return attachedEventReference;
            }
            foreach (var attachedPropertyReference in GetAttachedProperties(type, methodsLookUpTable))
            {
                yield return attachedPropertyReference;
            }
        }

        private static Dictionary<string, IEnumerable<MethodDefinition>> GetMethodsLookUpTable(TypeDefinition type)
        {
            return type.Methods.GroupBy(i => i.Name, i => i).ToDictionary(i => i.Key, i => i.AsEnumerable());
        }

        #region Attached Events
        private static IEnumerable<AttachedEventReference> GetAttachedEvents(TypeDefinition type, Dictionary<string, IEnumerable<MethodDefinition>> methods)
        {
            foreach (var field in type.Fields)
            {
                if (IsAttachedEvent(field, methods))
                    yield return new AttachedEventReference(field);
            }
        }

        private static bool IsAttachedEvent(FieldDefinition field, Dictionary<string, IEnumerable<MethodDefinition>> methods)
        {
            // https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/attached-events-overview
            if (!field.Name.EndsWith(EventConst))
                return false;
            var addMethodName = $"Add{GetEventName(field.Name)}Handler";
            var removeMethodName = $"Remove{GetEventName(field.Name)}Handler";
            return
                // WPF implements attached events as routed events; the identifier to use for an event (RoutedEvent) is already defined by the WPF event system
                (IsAssignableTo(field.FieldType, Consts.RoutedEventFullName) || 
                IsAssignableTo(field.FieldType, Consts.RoutedEventFullNameWinRT) || 
                IsAssignableTo(field.FieldType, Consts.RoutedEventFullNameWinUI))
                && field.IsPublic
                && field.IsStatic
                && field.IsInitOnly

                // Has a method Add*Handler with two parameters. 
                // Has a method Remove*Handler with two parameters. 
                && methods.ContainsKey(addMethodName)
                && methods.ContainsKey(removeMethodName)
                && methods[addMethodName].Any(IsAttachedEventMethod)
                && methods[removeMethodName].Any(IsAttachedEventMethod);
        }

        private static bool IsAttachedEventMethod(MethodDefinition method)
        {
            // The method must be public and static, with no return value.
            return method.IsPublic
                && method.IsStatic
                && method.ReturnType.FullName == Consts.VoidFullName
                && AreAttachedEventMethodParameters(method.Parameters);
        }

        private static bool AreAttachedEventMethodParameters(Collection<ParameterDefinition> parameters)
        {
            if (parameters.Count != 2)
                return false;
            return
                // The first parameter is DependencyObject
                (IsAssignableTo(parameters[0].ParameterType, Consts.DependencyObjectFullName) || 
                IsAssignableTo(parameters[0].ParameterType, Consts.DependencyObjectFullNameWinRT) ||
                IsAssignableTo(parameters[0].ParameterType, Consts.DependencyObjectFullNameWinUI))

                // The second parameter is the handler to add/remove
                && IsAttachedEventHandler(parameters[1].ParameterType);
        }

        private static bool IsAttachedEventHandler(TypeReference typeReference)
        {
            var type = typeReference.Resolve();
            if (!DocUtils.IsDelegate(type))
                return false;
            MethodDefinition invoke = type.GetMethod("Invoke");
            return invoke.Parameters.Count == 2;
        }
        #endregion

        #region Attached Properties
        private static IEnumerable<AttachedPropertyReference> GetAttachedProperties(TypeDefinition type, Dictionary<string, IEnumerable<MethodDefinition>> methods)
        {
            foreach (var field in type.Fields)
            {
                if (IsAttachedProperty(field, methods))
                    yield return new AttachedPropertyReference(field);
            }

            foreach (var property in type.Properties.Where(t => t.PropertyType.FullName == Consts.DependencyPropertyFullName
            || t.PropertyType.FullName == Consts.DependencyPropertyFullNameWindowsXaml
            || t.PropertyType.FullName == Consts.DependencyPropertyFullNameMicrosoftXaml))
            {
                if (IsAttachedProperty(property, methods))
                    yield return new AttachedPropertyReference(property);
            }
        }

        private static bool IsAttachedProperty(FieldDefinition field, Dictionary<string, IEnumerable<MethodDefinition>> methods)
        {
            // https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/attached-properties-overview
            // https://github.com/mono/api-doc-tools/issues/63#issuecomment-328995418
            if (!field.Name.EndsWith(PropertyConst, StringComparison.Ordinal))
                return false;
            var propertyName = GetPropertyName(field.Name);
            var getMethodName = $"Get{propertyName}";
            var setMethodName = $"Set{propertyName}";

            var hasDualPropertyConst = propertyName.EndsWith(PropertyConst, StringComparison.Ordinal);
            var hasExistingProperty = field?.DeclaringType?.Properties.Any (p => p.Name.Equals (propertyName, StringComparison.Ordinal) && GetCheckVisible(p.Resolve()));
            var hasExistingField = hasDualPropertyConst ? false : 
                field?.DeclaringType?.Fields.Any (f => f.Name.Equals (propertyName, StringComparison.Ordinal) && GetCheckVisible(f.Resolve()));

            return !hasExistingProperty.IsTrue () && !hasExistingField.IsTrue () &&
                // Class X has a static field of type DependencyProperty [Name]Property
                (field.FieldType.FullName == Consts.DependencyPropertyFullName || field.FieldType.FullName == Consts.DependencyPropertyFullNameWindowsXaml
                || field.FieldType.FullName == Consts.DependencyPropertyFullNameMicrosoftXaml)
                && field.IsPublic
                && field.IsStatic
                && field.IsInitOnly

                // Class X also has static methods with the following names: Get[Name] or Set[Name]
                && ((methods.ContainsKey(getMethodName) && methods[getMethodName].Any(IsAttachedPropertyGetMethod))
                    || (methods.ContainsKey(setMethodName) && methods[setMethodName].Any(IsAttachedPropertySetMethod)));

        }

        private static bool IsAttachedProperty(PropertyDefinition property, Dictionary<string, IEnumerable<MethodDefinition>> methods)
        {

            if (!property.Name.EndsWith(PropertyConst, StringComparison.Ordinal))
                return false;
            var propertyName = GetPropertyName(property.Name);
            var getMethodName = $"Get{propertyName}";
            var setMethodName = $"Set{propertyName}";

            var hasExistingProperty = property?.DeclaringType?.Properties.Any(p => p.Name.Equals(propertyName, StringComparison.Ordinal) && GetCheckVisible(p.Resolve()));
            var hasExistingField = property?.DeclaringType?.Fields.Any(f => f.Name.Equals(propertyName, StringComparison.Ordinal) && GetCheckVisible(f.Resolve()));

            return !hasExistingProperty.IsTrue() && !hasExistingField.IsTrue() &&
                // Class X has a static field of type DependencyProperty [Name]Property
                (property.PropertyType.FullName == Consts.DependencyPropertyFullName || property.PropertyType.FullName == Consts.DependencyPropertyFullNameWindowsXaml
                || property.PropertyType.FullName == Consts.DependencyPropertyFullNameMicrosoftXaml)


                // Class X also has static methods with the following names: Get[Name] or Set[Name]
                && ((methods.ContainsKey(getMethodName) && methods[getMethodName].Any(IsAttachedPropertyGetMethod))
                    || (methods.ContainsKey(setMethodName) && methods[setMethodName].Any(IsAttachedPropertySetMethod)));

        }

        private static bool IsAttachedPropertyGetMethod(MethodDefinition method)
        {
            return method.Parameters.Count == 1

                   // returns a value of type dp.PropertyType (or IsAssignableTo…), where dp is the value of the static field.
                   // && IsAssignableTo(method.ReturnType, "");

                   // The Get method takes one argument of type DependencyObject(or something IsAssignableTo(DependencyObject), 
                   && (IsAssignableTo(method.Parameters[0].ParameterType, Consts.DependencyObjectFullName) || 
                   IsAssignableTo(method.Parameters[0].ParameterType, Consts.DependencyObjectFullNameWinRT) ||
                   IsAssignableTo(method.Parameters[0].ParameterType, Consts.DependencyObjectFullNameWinUI));
        }

        private static bool IsAttachedPropertySetMethod(MethodDefinition method)
        {
            return method.Parameters.Count == 2// The Set method takes two arguments.
                   
                   // The first has type DependencyObject(or IsAssignableTo…), 
                   && (IsAssignableTo(method.Parameters[0].ParameterType, Consts.DependencyObjectFullName) || 
                   IsAssignableTo(method.Parameters[0].ParameterType, Consts.DependencyObjectFullNameWinRT) ||
                   IsAssignableTo(method.Parameters[0].ParameterType, Consts.DependencyObjectFullNameWinUI) ||
                   IsAssignableTo(method.Parameters[0].ParameterType, Consts.DependencyPropertyFullNameIInputElement) || 
                   IsAssignableTo(method.Parameters[0].ParameterType, Consts.DependencyPropertyFullNameObject))

                   // the second has type dp.PropertyType (or IsAssignableTo…).
                   // && IsAssignableTo(method.Parameters[1].ParameterType, "")

                   // It returns void.
                   && method.ReturnType.FullName == Consts.VoidFullName;
        }
        #endregion
        
        private static bool IsAssignableTo(TypeReference type, string targetTypeName)
        {
            if (type == null)
                return false;
            var typeDefenition = type.Resolve();
            if (typeDefenition == null || typeDefenition.IsSealed)
                return type.FullName == targetTypeName;

            return type.FullName == targetTypeName || IsAssignableTo(typeDefenition.BaseType, targetTypeName);
        }

        private static bool GetCheckVisible(IMemberDefinition member)
        {
            if (member == null)
                throw new ArgumentNullException("member");
            PropertyDefinition prop = member as PropertyDefinition;
            if (prop != null)
                return ChkPropertyVisible(prop);
            FieldDefinition field = member as FieldDefinition;
            if (field != null)
                return ChkFieldVisible(field);
            return false;
        }

        private static bool ChkPropertyVisible(PropertyDefinition property)
        {
            MethodDefinition method;
            bool get_visible = false;
            bool set_visible = false;

            if ((method = property.GetMethod) != null &&
                    (DocUtils.IsExplicitlyImplemented(method) ||
                     (!method.IsPrivate && !method.IsAssembly && !method.IsFamilyAndAssembly)))
                get_visible = true;

            if ((method = property.SetMethod) != null &&
                    (DocUtils.IsExplicitlyImplemented(method) ||
                     (!method.IsPrivate && !method.IsAssembly && !method.IsFamilyAndAssembly)))
                set_visible = true;

            if ((set_visible == false) && (get_visible == false))
                return false;
            else
                return true;
        }

        private static bool ChkFieldVisible(FieldDefinition field)
        {
            TypeDefinition declType = (TypeDefinition)field.DeclaringType;

            if (declType.IsEnum && field.Name == "value__")
                return false; // This member of enums aren't documented.

            return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

        }

    }
    internal static class NBoolExtensions
    {
        public static bool IsTrue (this Nullable<bool> value) => 
            value.HasValue && value.Value;
    }
}
