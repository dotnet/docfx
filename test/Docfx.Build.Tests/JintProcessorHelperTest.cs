// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Jint;

namespace Docfx.Build.Engine.Tests;

[TestClass]
public class JintProcessorHelperTest
{
    [TestProperty("Related", "JintProcessor")]
    [TestMethod]
    public void TestJObjectConvertWithJToken()
    {
        var testData = ConvertToObjectHelper.ConvertStrongTypeToObject(new TestData());
        {
            var engine = new Jint.Engine();
            var jsValue = JintProcessorHelper.ConvertObjectToJsValue(engine, testData);
            Assert.IsTrue(jsValue.IsObject());
            dynamic value = jsValue.ToObject();
            Assert.AreEqual(2, value.ValueA);
            Assert.AreEqual("ValueB", value.ValueB);
            System.Dynamic.ExpandoObject valueDict = value.ValueDict;
            var dict = (IDictionary<string, object>)valueDict;
            Assert.AreEqual("Value1", dict["1"]);
            Assert.AreEqual(2.0, dict["key"]);
            object[] array = value.ValueList;
            Assert.AreEqual("ValueA", array[0]);
            Assert.AreEqual("ValueB", array[1]);
        }
    }

    [TestProperty("Related", "JintProcessor")]
    [TestMethod]
    [DataRow("string", "string")]
    [DataRow(1, 1.0)]
    [DataRow(true, true)]
    [DataRow('a', "a")]
    public void TestJObjectConvertWithPrimaryType(object input, object expected)
    {
        var engine = new Jint.Engine();
        var jsValue = JintProcessorHelper.ConvertObjectToJsValue(engine, input);
        Assert.AreEqual(expected, jsValue.ToObject());
    }

    private sealed class TestData
    {
        public int ValueA { get; set; } = 2;

        public string ValueB { get; set; } = "ValueB";

        public Dictionary<object, object> ValueDict { get; set; } = new() { [1] = "Value1", ["key"] = 2 };

        public List<string> ValueList { get; set; } = ["ValueA", "ValueB"];
    }
}
