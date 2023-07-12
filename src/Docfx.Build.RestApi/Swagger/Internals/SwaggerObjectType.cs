// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.RestApi.Swagger.Internals;

internal enum SwaggerObjectType
{
    /// <summary>
    /// {
    ///     "$ref": "#/definitions/definition"
    ///     "otherProperty": "value"
    /// }
    /// </summary>
    ReferenceObject,

    /// <summary>
    /// {
    ///     "property1": "swaggerObject1",
    ///     "property2": "swaggerObject2"
    /// }
    /// </summary>
    Object,

    /// <summary>
    /// [
    ///     "swaggerObject1",
    ///     "swaggerObject2"
    /// ]
    /// </summary>
    Array,

    /// <summary>
    /// Value type similar to JValue
    /// </summary>
    ValueType,

    /// <summary>
    /// Loop reference type, to indicate to ignore when serializing
    /// </summary>
    LoopReference
}
