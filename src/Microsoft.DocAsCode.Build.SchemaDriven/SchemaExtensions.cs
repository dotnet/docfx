// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    public static class SchemaExtensions
    {
        private const string IsRequiredInFragmentsTag = "editable";

        private const string IsLegalInFragmentsTag = "editable";

        /// <summary>
        /// Return if a property is required to appear in markdown fragments
        /// </summary>
        public static bool IsRequiredInFragments(this BaseSchema schema)
            => schema.Tags.IndexOf(IsRequiredInFragmentsTag) >= 0;

        /// <summary>
        /// Return if a property is legal to appear in markdown fragmetns
        /// </summary>
        public static bool IsLegalInFragments(this BaseSchema schema)
            => schema.Tags.IndexOf(IsLegalInFragmentsTag) >= 0;
    }
}
