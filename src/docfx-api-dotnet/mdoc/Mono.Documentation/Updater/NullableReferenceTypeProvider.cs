using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.Documentation.Updater
{
    // About Nullable Reference Types: https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-reference-types.md
    // About Nullable Metadata: https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-metadata.md
    public class NullableReferenceTypeProvider
    {
        private const string NullableAttributeFullName = "System.Runtime.CompilerServices.NullableAttribute";
        private const string NullableContextAttributeFullName = "System.Runtime.CompilerServices.NullableContextAttribute";
        private const byte ObliviousNullableAttribute = 0;
        private const byte NotAnnotatedNullableAttribute = 1;
        private const byte AnnotatedNullableAttribute = 2;

        private ICustomAttributeProvider provider;

        public NullableReferenceTypeProvider(ICustomAttributeProvider provider)
        {
            this.provider = provider;
        }

        public IList<bool?> GetNullableReferenceTypeFlags()
        {
            var nullableAttribute = FindNullableAttribute();

            return GetTypeNullability(nullableAttribute);
        }

        private CustomAttribute FindNullableAttribute()
        {
            string findAttributeName = NullableAttributeFullName;
            foreach (var item in GetTypeNullableAttributes())
            {
                CustomAttribute nullableAttribute = FindCustomAttribute(item, findAttributeName);
                if (nullableAttribute != null)
                {
                    return nullableAttribute;
                }

                findAttributeName = NullableContextAttributeFullName;
            }

            return null;
        }

        private CustomAttribute FindCustomAttribute(ICustomAttributeProvider customAttributeProvider, string customAttributeName)
        {
            if (customAttributeProvider.HasCustomAttributes)
            {
                return customAttributeProvider.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName.Equals(customAttributeName));
            }

            return null;
        }

        private IList<bool?> GetTypeNullability(CustomAttribute typeCustomAttribute)
        {
            if (typeCustomAttribute == null)
            {
                return new List<bool?>
                {
                    null
                };
            }

            var nullableAttributeValue = typeCustomAttribute.ConstructorArguments[0].Value;
            if (nullableAttributeValue is CustomAttributeArgument[] nullableAttributeArguments)
            {
                return nullableAttributeArguments.Select(a => IsAnnotated((byte)a.Value)).ToList();
            }

            return new List<bool?>
            {
                IsAnnotated((byte)nullableAttributeValue)
            };
        }

        private bool? IsAnnotated(byte value)
        {
            switch (value)
            {
                case AnnotatedNullableAttribute:
                    return true;

                case NotAnnotatedNullableAttribute:
                    return false;

                case ObliviousNullableAttribute:
                    return null;
            }

            throw new ArgumentOutOfRangeException(nameof(value), value, $"The nullable attribute value is not a valid type argument.");
        }

        private ICollection<ICustomAttributeProvider> GetTypeNullableAttributes()
        {
            if (provider is ParameterDefinition parameterDefinition)
            {
                return GetTypeNullableAttributes(parameterDefinition);
            }

            if (provider is MethodReturnType methodReturnType)
            {
                return GetTypeNullableAttributes(methodReturnType);
            }

            if (provider is PropertyDefinition propertyDefinition)
            {
                return GetTypeNullableAttributes(propertyDefinition);
            }

            if (provider is FieldDefinition fieldDefinition)
            {
                return GetTypeNullableAttributes(fieldDefinition);
            }

            throw new ArgumentException("We don't support this custom attribute provider type now.", nameof(provider));
        }

        private ICollection<ICustomAttributeProvider> GetTypeNullableAttributes(ParameterDefinition parameterDefinition)
        {
            return GetTypeNullableAttributes(parameterDefinition, parameterDefinition.Method as MethodDefinition);
        }

        private ICollection<ICustomAttributeProvider> GetTypeNullableAttributes(MethodReturnType methodReturnType)
        {
            return GetTypeNullableAttributes(methodReturnType, methodReturnType.Method as MethodDefinition);
        }

        private ICollection<ICustomAttributeProvider> GetTypeNullableAttributes(PropertyDefinition propertyDefinition)
        {
            return GetTypeNullableAttributes(propertyDefinition, propertyDefinition.GetMethod);
        }

        private ICollection<ICustomAttributeProvider> GetTypeNullableAttributes(FieldDefinition fieldDefinition)
        {
            return new List<ICustomAttributeProvider>
            {
                fieldDefinition,
                fieldDefinition.DeclaringType
            };
        }

        private ICollection<ICustomAttributeProvider> GetTypeNullableAttributes(ICustomAttributeProvider customAttributeProvider, MethodDefinition methodDefinition)
        {
            var resultList = new List<ICustomAttributeProvider> 
            {
                customAttributeProvider
            };

            if (methodDefinition != null)
            {
                resultList.Add(methodDefinition);
                resultList.Add(methodDefinition.DeclaringType);
            }

            return resultList;
        }
    }
}
