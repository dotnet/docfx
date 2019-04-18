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
            ShouldNotSerialize();
            SetFieldWritable();
            return prop;

            void ShouldNotSerialize()
            {
                // SourceInfo with null value
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(SourceInfo<>))
                {
                    prop.ShouldSerialize =
                        target => ((SourceInfo)prop.ValueProvider.GetValue(target))?.GetValue() != null;
                }

                // Empty array
                else if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && !(prop.PropertyType == typeof(string)))
                {
                    prop.ShouldSerialize =
                    target =>
                    {
                        var value = prop.ValueProvider.GetValue(target);

                        if (value is IEnumerable enumer && !enumer.GetEnumerator().MoveNext())
                        {
                            return false;
                        }

                        return true;
                    };
                }
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
        }
    }
}
