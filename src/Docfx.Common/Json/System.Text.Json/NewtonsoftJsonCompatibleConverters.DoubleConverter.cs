// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core.Tokens;

#nullable enable

namespace Docfx.Common;

internal partial class NewtonsoftJsonCompatibleConverters
{
    internal class DoubleConverter : JsonConverter<double>
    {
        private const int MaximumFormatDoubleLength = 128; // System.Text.Json using this setting.

        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                return reader.GetDouble();

            return ReadStringAsNumber<double>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (TryHandleNamedFloatingPointLiterals(writer, value, options))
                return;

            if (options.NumberHandling.HasFlag(JsonNumberHandling.WriteAsString))
                throw new NotSupportedException("JsonNumberHandling.WriteAsString option is not supported.");

            // Allocate temp buffer
            Span<byte> buffer = stackalloc byte[MaximumFormatDoubleLength + 2];
            Utf8Formatter.TryFormat(value, buffer, out var bytesWritten, StandardFormatGeneral);

            // If number contains period or exponential. Use default WriteNumberValue implementation
            if (buffer.IndexOfAny("E."u8) != -1)
            {
                writer.WriteNumberValue(value);
                return;
            }

            // Append `.0` suffix
            buffer[bytesWritten++] = (byte)'.';
            buffer[bytesWritten++] = (byte)'0';

            // Write value with WriteRawValue API.
            bool needIndent = writer.Options.Indented && GetTokenType(writer) != JsonTokenType.PropertyName;
            if (needIndent)
                WriteRawValueIndent(writer, buffer[..bytesWritten]);
            else
                writer.WriteRawValue(buffer[..bytesWritten], skipInputValidation: true);
        }
    }
}
