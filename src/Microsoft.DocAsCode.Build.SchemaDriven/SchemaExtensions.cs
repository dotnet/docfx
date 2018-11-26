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
        /// Return if a property is required to appear in markdown fragments
        /// </summary>
        public static bool IsRequiredInFragments(this BaseSchema schema) => IsEditable(schema);

        /// <summary>
        /// Return if a property is legal to appear in markdown fragmetns
        /// </summary>
        public static bool IsLegalInFragments(this BaseSchema schema) => IsEditable(schema);

        private static bool IsEditable(this BaseSchema schema)
        {
            if (schema?.ContentType == ContentType.Markdown)
            {
                return true;
            }
            if (schema?.Tags == null)
            {
                return false;
            }

            return schema.Tags.Contains(IsEditableTag, StringComparer.OrdinalIgnoreCase);
        }
    }
}
