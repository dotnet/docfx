// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.Linq;

    public static class SchemaExtensions
    {
        private const string IsEditableTag = "editable";

        /// <summary>
        /// Return if a property is editable
        /// </summary>
        public static bool IsEditable(this BaseSchema schema)
        {
            if (schema?.Tags == null)
            {
                return false;
            }

            return schema.Tags.Contains(IsEditableTag, StringComparer.OrdinalIgnoreCase);
        }
    }
}
