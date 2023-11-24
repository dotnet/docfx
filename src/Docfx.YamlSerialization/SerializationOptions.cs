// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.YamlSerialization;

/// <summary>Options that control the serialization process.</summary>
[Flags]
public enum SerializationOptions
{
    /// <summary>Serializes using the default options</summary>
    None = 0,
    /// <summary>
    /// Ensures that it will be possible to deserialize the serialized objects.
    /// </summary>
    Roundtrip = 1,
    /// <summary>
    /// If this flag is specified, if the same object appears more than once in the
    /// serialization graph, it will be serialized each time instead of just once.
    /// </summary>
    /// <remarks>
    /// If the serialization graph contains circular references and this flag is set,
    /// a StackOverflowException will be thrown.
    /// If this flag is not set, there is a performance penalty because the entire
    /// object graph must be walked twice.
    /// </remarks>
    DisableAliases = 2,
    /// <summary>
    /// Forces every value to be serialized, even if it is the default value for that type.
    /// </summary>
    EmitDefaults = 4,
    /// <summary>
    /// Ensures that the result of the serialization is valid JSON.
    /// </summary>
    JsonCompatible = 8,
    /// <summary>
    /// Use the static type of values instead of their actual type.
    /// </summary>
    DefaultToStaticType = 16, // 0x00000010
}
