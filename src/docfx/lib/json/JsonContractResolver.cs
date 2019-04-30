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
            var prop = base.CreateProperty(member, memberSerialization);
            ShouldNotSerializeEmptySourceInfo();
            ShouldNotSerializeEmptyArray();
            SetFieldWritable();
            AutoExpandArrays();
            return prop;

            // For SourceInfo<>, with value of empty array or null should not be serialized
            void ShouldNotSerializeEmptySourceInfo()
            {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(SourceInfo<>))
                {
                    prop.ShouldSerialize =
                        target =>
                        {
                            var value = prop.ValueProvider.GetValue(target);
                            if (((SourceInfo)value)?.GetValue() is null)
                                return false;

                            if (IsEmptyArray(((SourceInfo)value).GetValue()))
                            {
                                return false;
                            }
                            return true;
                        };
                }
            }

            void ShouldNotSerializeEmptyArray()
            {
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && !(prop.PropertyType == typeof(string)))
                {
                    prop.ShouldSerialize =
                        target => !IsEmptyArray(prop.ValueProvider.GetValue(target));
                }
            }

            bool IsEmptyArray(object value)
            {
                if (value is IEnumerable enumer && !enumer.GetEnumerator().MoveNext())
                {
                    return true;
                }
                return false;
            }

            void SetFieldWritable()
            {
                if (!prop.Writable)
                {
                    if (member is FieldInfo f && f.IsPublic && !f.IsStatic)
                    {
                        prop.Writable = true;
                    }
                }
            }

            void AutoExpandArrays()
            {
                if (prop.Converter == null && prop.PropertyType.IsArray)
                {
                    prop.Converter = new ExpandArrayJsonConverter();
                }
            }
        }
    }
}
