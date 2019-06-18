// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    internal class JsonContractResolver : DefaultContractResolver
    {
        // HACK: Json.NET property deserialization is case insensitive:
        // https://github.com/JamesNK/Newtonsoft.Json/issues/815,
        // Force property deserialization to be case sensitive by hijacking GetClosestMatchProperty implementation.
        private static readonly Action<JsonPropertyCollection, List<JsonProperty>> s_makeJsonCaseSensitive =
            ReflectionUtility.CreateInstanceFieldSetter<JsonPropertyCollection, List<JsonProperty>>("_list");

        private static readonly List<JsonProperty> s_emptyPropertyList = new List<JsonProperty>();

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);
            s_makeJsonCaseSensitive(contract.Properties, s_emptyPropertyList);
            return contract;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            HandleSourceInfo(property);
            DoNotSerializeEmptyArray(property);
            SetFieldWritable();
            return property;

            void SetFieldWritable()
            {
                if (!property.Writable)
                {
                    if (member is FieldInfo f && f.IsPublic && !f.IsStatic)
                    {
                        property.Writable = true;
                    }
                }
            }
        }

        private void DoNotSerializeEmptyArray(JsonProperty property)
        {
            if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && !(property.PropertyType == typeof(string)))
            {
                var originalShouldSerialize = property.ShouldSerialize;

                property.ShouldSerialize = target =>
                {
                    if (IsEmptyArray(property.ValueProvider.GetValue(target)))
                    {
                        return false;
                    }

                    return originalShouldSerialize?.Invoke(target) ?? true;
                };
            }
        }

        private static bool IsEmptyArray(object value)
        {
            return !(value is string) && value is IEnumerable enumerable && !enumerable.GetEnumerator().MoveNext();
        }

        private static void HandleSourceInfo(JsonProperty property)
        {
            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(SourceInfo<>))
            {
                // Allow source info propagation for null values
                property.NullValueHandling = NullValueHandling.Include;

                // Do not serialize empty array or null
                var originalShouldSerialize = property.ShouldSerialize;

                property.ShouldSerialize = target =>
                {
                    var sourceInfoValue = property.ValueProvider.GetValue(target);
                    var value = ((ISourceInfo)sourceInfoValue).GetValue();

                    if (value is null || IsEmptyArray(value))
                    {
                        return false;
                    }

                    return originalShouldSerialize?.Invoke(target) ?? true;
                };
            }
        }
    }
}
