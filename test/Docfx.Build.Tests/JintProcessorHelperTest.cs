// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Jint;
using Xunit;

namespace Docfx.Build.Engine.Tests;

public class JintProcessorHelperTest
{
    [Trait("Related", "JintProcessor")]
    [Fact]
    public void TestJObjectConvertWithJToken()
    {
        var testData = ConvertToObjectHelper.ConvertStrongTypeToObject(new TestData());
        {
            var engine = new Jint.Engine();
            var jsValue = JintProcessorHelper.ConvertObjectToJsValue(engine, testData);
            Assert.True(jsValue.IsObject());
            dynamic value = jsValue.ToObject();
            Assert.Equal(2, value.ValueA);
            Assert.Equal("ValueB", value.ValueB);
            System.Dynamic.ExpandoObject valueDict = value.ValueDict;
            var dict = (IDictionary<string, object>)valueDict;
            Assert.Equal("Value1", dict["1"]);
            Assert.Equal(2.0, dict["key"]);
            object[] array = value.ValueList;
            Assert.Equal("ValueA", array[0]);
            Assert.Equal("ValueB", array[1]);
        }
    }

    [Trait("Related", "JintProcessor")]
    [Fact]
    public void TestObjectConvertKeepsDeclarationOrder()
    {
        var engine = new Jint.Engine();
        var model = new Dictionary<string, object>
        {
            ["title"] = "Hello",
            ["nested"] = new Dictionary<string, object> { ["a"] = 1, ["b"] = "two" },
            ["items"] = new List<object> { "x", "y" },
        };

        engine.SetValue("model", JintProcessorHelper.ConvertObjectToJsValue(engine, model));

        Assert.Equal("title,nested,items", engine.Evaluate("Object.keys(model).join(',')").AsString());
        Assert.Equal("Hello", engine.Evaluate("model.title").AsString());
        Assert.Equal("a,b", engine.Evaluate("Object.keys(model.nested).join(',')").AsString());
        Assert.Equal("two", engine.Evaluate("model.nested.b").AsString());
        Assert.Equal("x,y", engine.Evaluate("model.items.join(',')").AsString());
    }

    /// <summary>
    /// Documents of one document type share a model layout, which is what the conversion is built around.
    /// Objects sharing a layout must stay indistinguishable from separately built ones.
    /// </summary>
    [Trait("Related", "JintProcessor")]
    [Fact]
    public void TestObjectConvertIsStableAcrossModelsOfTheSameShape()
    {
        var engine = new Jint.Engine();

        for (var i = 0; i < 3; i++)
        {
            var model = new Dictionary<string, object> { ["uid"] = $"uid{i}", ["type"] = "Class" };
            engine.SetValue($"model{i}", JintProcessorHelper.ConvertObjectToJsValue(engine, model));
        }

        Assert.Equal("uid,type", engine.Evaluate("Object.keys(model0).join(',')").AsString());
        Assert.Equal("uid0|uid1|uid2", engine.Evaluate("[model0, model1, model2].map(function (m) { return m.uid; }).join('|')").AsString());
        Assert.True(engine.Evaluate("[model0, model1, model2].every(function (m) { return m.type === 'Class'; })").AsBoolean());
    }

    /// <summary>
    /// Integer-index-like keys cannot be expressed in the layout the conversion targets, so they take a
    /// fallback path. It has to stay silent and correct.
    /// </summary>
    [Trait("Related", "JintProcessor")]
    [Fact]
    public void TestObjectConvertWithIntegerLikeKeys()
    {
        var engine = new Jint.Engine();
        var model = new Dictionary<string, object>
        {
            ["name"] = "n",
            ["1"] = "one",
            ["2"] = "two",
        };

        engine.SetValue("model", JintProcessorHelper.ConvertObjectToJsValue(engine, model));

        // Integer-like own keys are enumerated first in ascending order, exactly as for an object literal.
        Assert.Equal("1,2,name", engine.Evaluate("Object.keys(model).join(',')").AsString());
        Assert.Equal("one", engine.Evaluate("model['1']").AsString());
        Assert.Equal("n", engine.Evaluate("model.name").AsString());
    }

    [Trait("Related", "JintProcessor")]
    [Theory]
    [InlineData("string", "string")]
    [InlineData(1, 1.0)]
    [InlineData(true, true)]
    [InlineData('a', "a")]
    public void TestJObjectConvertWithPrimaryType(object input, object expected)
    {
        var engine = new Jint.Engine();
        var jsValue = JintProcessorHelper.ConvertObjectToJsValue(engine, input);
        Assert.Equal(expected, jsValue.ToObject());
    }

    private sealed class TestData
    {
        public int ValueA { get; set; } = 2;

        public string ValueB { get; set; } = "ValueB";

        public Dictionary<object, object> ValueDict { get; set; } = new() { [1] = "Value1", ["key"] = 2 };

        public List<string> ValueList { get; set; } = ["ValueA", "ValueB"];
    }
}
