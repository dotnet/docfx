// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx.Common.Tests;

[TestProperty("Related", "ConvertToObjectHelper")]
[TestClass]
public class ConvertToObjectHelperTest
{
    [TestMethod]
    [DataRow(1, typeof(int))]
    [DataRow(1L, typeof(long))]
    [DataRow(1.0, typeof(double))]
    [DataRow("string", typeof(string))]
    [DataRow('c', typeof(string))]
    public void ConvertSimpleTypeToObjectShouldWork(object value, Type expectedType)
    {
        var result = ConvertToObjectHelper.ConvertStrongTypeToObject(value);
        Assert.AreEqual(expectedType, result.GetType());
    }

    [TestMethod]
    public void ConvertComplexTypeToObjectShouldWork()
    {
        var complexType = new ComplexType
        {
            String = "String",
            List = [],
            IntDictionary = []
        };
        var result = ConvertToObjectHelper.ConvertStrongTypeToObject(complexType);
        Assert.AreEqual(typeof(Dictionary<string, object>), result.GetType());
        Assert.AreEqual(typeof(object[]), ((Dictionary<string, object>)result)["List"].GetType());
        Assert.AreEqual(typeof(Dictionary<string, object>), ((Dictionary<string, object>)result)["IntDictionary"].GetType());
    }

    [TestMethod]
    public void ConvertComplexTypeWithJsonAttributeToObjectShouldUseAttributeAsPropertyName()
    {
        var complexType = new ComplexTypeWithJson
        {
            String = "String",
            List = [],
            IntDictionary = []
        };
        var result = ConvertToObjectHelper.ConvertStrongTypeToObject(complexType);
        Assert.AreEqual(typeof(Dictionary<string, object>), result.GetType());
        Assert.AreEqual(typeof(object[]), ((Dictionary<string, object>)result)["list"].GetType());
        Assert.AreEqual(typeof(Dictionary<string, object>), ((Dictionary<string, object>)result)["dict"].GetType());
    }

    [TestMethod]
    public void ConvertObjectWithCircularReferenceToDynamic()
    {
        var a = new Dictionary<string, object>
        {
            ["key"] = "value"
        };
        a["key1"] = a;

        dynamic converted = ConvertToObjectHelper.ConvertToDynamic(a);
        Assert.AreSame(converted.key1, converted);
        Assert.AreEqual("value", converted.key1.key);

        Dictionary<string, object> obj = (Dictionary<string, object>)ConvertToObjectHelper.ConvertExpandoObjectToObject(converted);
        Assert.IsTrue(ReferenceEquals(obj["key1"], obj));
        Assert.AreEqual("value", ((Dictionary<string, object>)obj["key1"])["key"]);
    }

    [TestMethod]
    public void ConvertJObjectToObject_UnexpectedType()
    {
        // Arrange
        var jToken = new JProperty("name", "dummy");

        // Act
        var action = () => { ConvertToObjectHelper.ConvertJObjectToObject(jToken); };

        // Assert
        action.Should()
              .Throw<ArgumentException>()
              .WithMessage("Not expected object type passed. JTokenType: Property, Text: \"name\": \"dummy\"");
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
        [JsonPropertyName("str")]
        public string String { get; set; }

        [JsonProperty("list")]
        [JsonPropertyName("list")]
        public List<string> List { get; set; }

        [JsonProperty("dict")]
        [JsonPropertyName("dict")]
        public Dictionary<int, string> IntDictionary { get; set; }
    }
}
