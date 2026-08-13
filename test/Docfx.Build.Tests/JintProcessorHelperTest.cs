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

    /// <summary>
    /// Jint's default for CLR arrays became a live view in 4.14: script writing to such an array writes
    /// through to the CLR array itself. This bump crosses that change, and it is inert here only because
    /// <see cref="JintProcessorHelper.ConvertObjectToJsValue"/> builds every array itself and never hands a
    /// CLR array to Jint's converter. What keeps that true is that model arrays arrive as
    /// <c>IList&lt;object&gt;</c> - <c>ConvertToObjectHelper</c> produces <c>object[]</c>, which qualifies -
    /// so a model conversion that ever yielded a typed array such as <c>int[]</c> would silently start
    /// handing script a live view onto docfx's own model. The counters make that observable.
    /// </summary>
    [Trait("Related", "JintProcessor")]
    [Fact]
    public void TestObjectConvertCrossesNoClrArrays()
    {
        var engine = new Jint.Engine();
        var model = ConvertToObjectHelper.ConvertStrongTypeToObject(new TestData());

        engine.SetValue("model", JintProcessorHelper.ConvertObjectToJsValue(engine, model));

        // Read the arrays from script, so a conversion deferred to first access would still be counted.
        Assert.Equal("ValueA,ValueB", engine.Evaluate("model.ValueList.join(',')").AsString());

        var diagnostics = engine.Advanced.GetInteropConversionDiagnostics();
        Assert.Equal(0, diagnostics.ArrayLiveViewConversions);
        Assert.Equal(0, diagnostics.ArrayCopyConversions);

        // Control, so the zeros above cannot pass vacuously: a CLR array that does reach Jint's converter is
        // counted. Summing the two modes keeps this independent of which one is the current default.
        engine.SetValue("clrArray", new[] { 1, 2, 3 });
        var control = engine.Advanced.GetInteropConversionDiagnostics();
        Assert.Equal(1, control.ArrayLiveViewConversions + control.ArrayCopyConversions);
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

        // The point of building models this way is that documents of one document type share a hidden class,
        // and the conversion falls back to a per-object property dictionary silently when it cannot reach one.
        // HasSharedShape is the predicate Jint documents as stable for exactly this question - "is this
        // object's own storage a layout shared with its siblings" - and which a host may therefore pin for the
        // documented success case of the factory it called. If a future change stops these models being
        // shaped, this is how docfx finds out.
        for (var i = 0; i < 3; i++)
        {
            Assert.True(engine.Advanced.HasSharedShape(engine.Evaluate($"model{i}").AsObject()));
        }
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

        // Unlike the HasSharedShape assertion above, this one pins a *limitation* rather than a property, and
        // deliberately keeps using the finer-grained non-contract diagnostic, because naming the exact
        // fallback representation is the whole point of the assertion:
        // integer-index-like keys are something today's Jint cannot express in a hidden class, so the
        // conversion falls back. If a future Jint learns to shape them - a pure improvement for docfx - this
        // line will go red on good news, not on a docfx defect. The assertions above it already cover
        // correctness; delete this one at that point rather than working around it.
        Assert.Equal(
            ObjectRepresentation.Dictionary,
            engine.Advanced.GetObjectRepresentation(engine.Evaluate("model").AsObject()));
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
