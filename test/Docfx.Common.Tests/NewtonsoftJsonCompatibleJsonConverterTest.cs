// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using System.Text.Json;
using System.Text;
using Xunit;
using System.Text.Json.Serialization;

namespace Docfx.Common.Tests;

public class NewtonsoftJsonCompatibleJsonConverterTest
{
    [Theory]
    [InlineData(0f, "0.0")]
    [InlineData(1.0f, "1.0")]
    [InlineData(1.2345678f, "1.2345678")]
    [InlineData(-1.2345678f, "-1.2345678")]
    [InlineData(1234e5f, "123400000.0")]
    [InlineData(1234E-5f, "0.01234")]
    [InlineData(float.E, "2.7182817")]
    [InlineData(float.Epsilon, "1E-45")]
    [InlineData(float.MinValue, "-3.4028235E+38")]
    [InlineData(float.MaxValue, "3.4028235E+38")]
    // Following values are rounded by "G" format.
    // https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings#general-format-specifier-g
    [InlineData(float.Pi, "3.1415927")]  // Defined as `3.14159265`
    [InlineData(float.Tau, "6.2831855")] // Defined as `6.283185307`
    // Following values are serialized to string
    [InlineData(float.NaN, "\"NaN\"")]
    [InlineData(float.PositiveInfinity, "\"Infinity\"")]
    [InlineData(float.NegativeInfinity, "\"-Infinity\"")]
    public void FloatConverterTest(float value, string expected)
    {
        // Arrange
        var converter = new NewtonsoftJsonCompatibleConverters.FloatConverter();
        var options = SystemTextJsonUtility.DefaultSerializerOptions;

        // Act
        var json = Serialize(value, converter, options);
        var result = Deserialize<float>(json, converter, options);
        var roundtripJson = Serialize(result, converter, options);

        // Assert
        json.Should().Be(expected);
        json.Should().Be(roundtripJson);
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0d, "0.0")]
    [InlineData(1.0d, "1.0")]
    [InlineData(1.2345678901234567d, "1.2345678901234567")]
    [InlineData(-1.2345678901234567d, "-1.2345678901234567")]
    [InlineData(1234e5d, "123400000.0")]
    [InlineData(1234E-5d, "0.01234")]
    [InlineData(double.MinValue, "-1.7976931348623157E+308")]
    [InlineData(double.MaxValue, "1.7976931348623157E+308")]
    // Following values are rounded by "G" format.
    // https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings#general-format-specifier-g
    [InlineData(double.Epsilon, "5E-324")]        // Defined as `4.9406564584124654E-324`
    [InlineData(double.E, "2.718281828459045")]   // Defined as `2.7182818284590452354`
    [InlineData(double.Pi, "3.141592653589793")]  // Defined as `3.14159265358979323846`
    [InlineData(double.Tau, "6.283185307179586")] // Defined as `6.283185307179586476925`
    // Following values are serialized to string
    [InlineData(double.NaN, "\"NaN\"")]
    [InlineData(double.PositiveInfinity, "\"Infinity\"")]
    [InlineData(double.NegativeInfinity, "\"-Infinity\"")]
    public void DoubleConverterTest(double value, string expected)
    {
        // Arrange
        var converter = new NewtonsoftJsonCompatibleConverters.DoubleConverter();
        var options = SystemTextJsonUtility.DefaultSerializerOptions;

        // Act
        var json = Serialize(value, converter, options);
        var result = Deserialize<double>(json, converter, options);
        var roundtripJson = Serialize(result, converter, options);

        // Assert
        json.Should().Be(expected);
        json.Should().Be(roundtripJson);
        result.Should().Be(value);
    }

    [Fact]
    public void NumberConverterIndentsTest()
    {
        // Arrange
        var model = new TestData
        {
            FloatValue = 1f,
            DoubleValue = 2d,
            FloatValues = [1f, 2f, 3f],
            DoubleValues = [1d, 2d, 3d],
        };

        // Act
        var json = JsonUtility.Serialize(model, indented: true);
        var result = JsonUtility.Deserialize<TestData>(new StringReader(json));
        var roundtripJson = JsonUtility.Serialize(result, indented: true);

        var newtonSoftJson = NewtonsoftJsonUtility.Serialize(model, Newtonsoft.Json.Formatting.Indented);

        // Assert
        json.Should().Be(roundtripJson);
        result.Should().BeEquivalentTo(model);

        json.Should().Be(newtonSoftJson);
    }

    private class TestData
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "floatValue")]
        public float FloatValue { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "doubleValue")]
        public double DoubleValue { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "floatValues")]
        public float[] FloatValues { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "doubleValues")]
        public double[] DoubleValues { get; set; }
    }

    private static string Serialize<T>(T value, JsonConverter<T> converter, JsonSerializerOptions options)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(memoryStream);
        converter.Write(writer, value, options);
        writer.Flush();
        var bytes = memoryStream.ToArray();
        return Encoding.UTF8.GetString(bytes);
    }

    private static T Deserialize<T>(string json, JsonConverter<T> converter, JsonSerializerOptions options)
    {
        ReadOnlySpan<byte> span = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(span);
        reader.Read();

        return converter.Read(ref reader, typeof(T), options);
    }
}
