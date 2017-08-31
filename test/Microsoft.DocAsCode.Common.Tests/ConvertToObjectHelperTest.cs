// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    using Newtonsoft.Json;
    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("Related", "ConvertToObjectHelper")]
    public class ConvertToObjectHelperTest
    {
        [Theory]
        [InlineData(1, typeof(int))]
        [InlineData(1L, typeof(long))]
        [InlineData(1.0, typeof(double))]
        [InlineData("string", typeof(string))]
        [InlineData('c', typeof(string))]
        public void ConvertSimpleTypeToObjectShouldWork(object value, Type expectedType)
        {
            var result = ConvertToObjectHelper.ConvertStrongTypeToObject(value);
            Assert.Equal(expectedType, result.GetType());
        }

        [Fact]
        public void ConvertComplexTypeToObjectShouldWork()
        {
            var complexType = new ComplexType
            {
                String = "String",
                List = new List<string>(),
                IntDictionary = new Dictionary<int, string>()
            };
            var result = ConvertToObjectHelper.ConvertStrongTypeToObject(complexType);
            Assert.Equal(typeof(Dictionary<string, object>), result.GetType());
            Assert.Equal(typeof(object[]), ((Dictionary<string, object>)result)["List"].GetType());
            Assert.Equal(typeof(Dictionary<string, object>), ((Dictionary<string, object>)result)["IntDictionary"].GetType());
        }

        [Fact]
        public void ConvertComplexTypeWithJsonAttributeToObjectShouldUseAttributeAsPropertyName()
        {
            var complexType = new ComplexTypeWithJson
            {
                String = "String",
                List = new List<string>(),
                IntDictionary = new Dictionary<int, string>()
            };
            var result = ConvertToObjectHelper.ConvertStrongTypeToObject(complexType);
            Assert.Equal(typeof(Dictionary<string, object>), result.GetType());
            Assert.Equal(typeof(object[]), ((Dictionary<string, object>)result)["list"].GetType());
            Assert.Equal(typeof(Dictionary<string, object>), ((Dictionary<string, object>)result)["dict"].GetType());
        }

        [Fact]
        public void ConvertObjectWithCircularReferenceToDynamic()
        {
            var a = new Dictionary<string, object>
            {
                ["key"] = "value"
            };
            a["key1"] = a;

            dynamic converted = ConvertToObjectHelper.ConvertToDynamic(a);
            Assert.Equal(converted.key1, converted);
            Assert.Equal("value", converted.key1.key);

            Dictionary<string, object> obj = ConvertToObjectHelper.ConvertExpandoObjectToObject(converted);
            Assert.True(ReferenceEquals(obj["key1"], obj));
            Assert.Equal("value", ((Dictionary<string, object>)obj["key1"])["key"]);
        }

        private sealed class ComplexType
        {
            public string String { get; set; }
            public List<string> List { get; set; }
            public Dictionary<int, string> IntDictionary { get; set; }
        }

        private sealed class ComplexTypeWithJson
        {
            [JsonProperty("str")]
            public string String { get; set; }
            [JsonProperty("list")]
            public List<string> List { get; set; }
            [JsonProperty("dict")]
            public Dictionary<int, string> IntDictionary { get; set; }
        }
    }
}
