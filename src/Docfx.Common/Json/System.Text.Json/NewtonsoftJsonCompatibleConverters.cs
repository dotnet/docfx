// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace Docfx.Common;

internal partial class NewtonsoftJsonCompatibleConverters
{
    // System.Text.Json write float/double with "G" format. (Newtonsoft.Json using "R" format)
    private static readonly StandardFormat StandardFormatGeneral = StandardFormat.Parse("G");

    private static readonly JsonEncodedText EncodedNaN = JsonEncodedText.Encode("NaN");
    private static readonly JsonEncodedText EncodedPositiveInfinity = JsonEncodedText.Encode("Infinity");
    private static readonly JsonEncodedText EncodedNegativeInfinity = JsonEncodedText.Encode("-Infinity");

    // Try to hande NaN/PositiveInfinity/NegativeInfinity
    private static bool TryHandleNamedFloatingPointLiterals<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        where T : IFloatingPointIeee754<T>
    {
        if (!options.NumberHandling.HasFlag(JsonNumberHandling.AllowNamedFloatingPointLiterals))
            return false;

        // Try to handle Nan/PositiveInfinity/NegativeInfinity
        if (T.IsNaN(value))
        {
            writer.WriteStringValue(EncodedNaN);
            return true;
        }

        if (T.IsPositiveInfinity(value))
        {
            writer.WriteStringValue(EncodedPositiveInfinity);
            return true;
        }

        if (T.IsNegativeInfinity(value))
        {
            writer.WriteStringValue(EncodedNegativeInfinity);
            return true;
        }

        return false;
    }

    private static T ReadStringAsNumber<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
        where T : IFloatingPointIeee754<T>
    {
        // Try to parse NaN/PositiveInfinity/NegativeInfinity
        if (options.NumberHandling.HasFlag(JsonNumberHandling.AllowNamedFloatingPointLiterals))
        {
            if (reader.ValueTextEquals("NaN"))
                return T.NaN;
            if (reader.ValueTextEquals("Infinity"))
                return T.PositiveInfinity;
            if (reader.ValueTextEquals("-Infinity"))
                return T.NegativeInfinity;
        }

        // Try to parse text as number
        if (options.NumberHandling.HasFlag(JsonNumberHandling.AllowReadingFromString))
        {
            ReadOnlySpan<byte> valueSpan = reader.HasValueSequence
                ? reader.ValueSequence.ToArray()
                : reader.ValueSpan;

            if (!T.TryParse(valueSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                var numberText = reader.GetString();
                throw new JsonException($"Unable to parse text({numberText}) as {typeof(T).FullName}.");
            }

            return result;
        }

        // Failed to parse string as number. throw InvalidOperationException by expected value.
        var type = typeof(T);
        if (type == typeof(float)) reader.GetSingle();
        if (type == typeof(double)) reader.GetDouble();

        throw new UnreachableException($"Unexpected generic type parameter({type.FullName})");
    }

    // Write number with emmulating `Utf8JsonWriter.WriteNumberValueIndented` behavior.
    private static void WriteRawValueIndent(Utf8JsonWriter writer, ReadOnlySpan<byte> numberData)
    {
        Debug.Assert(writer.Options.Indented);

#if NET9_0_OR_GREATER
        var options = writer.Options;
        var newLineLength = options.NewLine.Length;
        var indentLength = options.IndentSize * writer.CurrentDepth;
#else
        var newLineLength = Environment.NewLine.Length;
        var indentLength = 2 * writer.CurrentDepth;
#endif

        // Allocate buffer
        Span<byte> buffer = stackalloc byte[newLineLength + (writer.CurrentDepth * 2) + numberData.Length];
        int bytesWritten = 0;

        // Add newline chars
        if (newLineLength == 2)
            buffer[bytesWritten++] = (byte)'\r';
        buffer[bytesWritten++] = (byte)'\n';

        // Add spaces for indent
        for (int i = 0; i < writer.CurrentDepth; ++i)
        {
            buffer[bytesWritten++] = (byte)' ';
            buffer[bytesWritten++] = (byte)' ';
        }

        // Copy number bytes to buffer
        numberData.CopyTo(buffer[bytesWritten..]);

        writer.WriteRawValue(buffer, skipInputValidation: true);
    }

    // Helper method to access `Utf8JsonWriter.TokenType` property.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_TokenType")]
    public static extern JsonTokenType GetTokenType(Utf8JsonWriter writer);
}
