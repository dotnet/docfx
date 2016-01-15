// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility.Tests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Utility;

    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("Related", "ConvertToObjectHelper")]
    public class ConvertToObjectHelperTest
    {
        [Theory]
        [InlineData(1, typeof(long))]
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

        private sealed class ComplexType
        {
            public string String { get; set; }
            public List<string> List { get; set; }
            public Dictionary<int, string> IntDictionary { get; set; }
        }

        private sealed class ComplexTypeWithJson
        {
            [Newtonsoft.Json.JsonProperty("str")]
            public string String { get; set; }
            [Newtonsoft.Json.JsonProperty("list")]
            public List<string> List { get; set; }
            [Newtonsoft.Json.JsonProperty("dict")]
            public Dictionary<int, string> IntDictionary { get; set; }
        }
    }
}
