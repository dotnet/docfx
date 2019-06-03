// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Exports a type to be processed by build pipeline into a JSON model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DataSchemaAttribute : Attribute { }

    /// <summary>
    /// Exports a type to be processed by build pipeline into an HTML page.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PageSchemaAttribute : DataSchemaAttribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class DataTypeAttribute : Attribute
    {
        public virtual Type TargetType => typeof(string);
    }
}
