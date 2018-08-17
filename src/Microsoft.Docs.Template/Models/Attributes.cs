// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Exports a type to be used for schema document processing
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DataSchemaAttribute : Attribute { }
}
