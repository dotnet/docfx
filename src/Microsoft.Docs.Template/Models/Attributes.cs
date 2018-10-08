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

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public abstract class DataTypeAttribute : Attribute
    {
        private int _order = -1;

        public virtual Type TargetType => typeof(string);

        public int Order
        {
            get => _order;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(nameof(Order));
                }

                _order = value;
            }
        }
    }

    public class HrefAttribute : DataTypeAttribute { }

    public class MarkdownAttribute : DataTypeAttribute { }

    public class InlineMarkdownAttribute : DataTypeAttribute { }

    public class HtmlAttribute : DataTypeAttribute { }

    public class XrefAttribute : DataTypeAttribute { }

    public class XrefPropertyAttribute : DataTypeAttribute { }
}
