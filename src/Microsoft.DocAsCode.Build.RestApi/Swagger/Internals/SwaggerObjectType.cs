// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
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
}
